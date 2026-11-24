using System.Globalization;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ModelContextProtocol.AspNetCore;
using VeterinerKlinigi.API;
using VeterinerKlinigi.API.Auth;
using VeterinerKlinigi.Business;
using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Business.Options;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Data.Context;
using VeterinerKlinigi.Data.Seed;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Web.Infrastructure;
using VeterinerKlinigi.Web.Mcp;

// =====================================================================
// Program.cs — Uygulamanın giriş noktası ve "kompozisyon kökü".
// Tüm katmanlar (Business, API, Data, Identity, MCP) burada
// birbirine bağlanır. İki bölümden oluşur:
//   1) builder.Services  → Dependency Injection (DI) kayıtları
//   2) app.Use... / Map... → HTTP isteğinin geçtiği middleware hattı
// =====================================================================

var builder = WebApplication.CreateBuilder(args);


var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Veritabanı bağlantı dizesi bulunamadı. appsettings.json içinde 'ConnectionStrings:DefaultConnection' tanımlayın.");
builder.Services.AddDataLayer(connectionString);

builder.Services.AddBusinessLayer();
builder.Services.Configure<AppointmentOptions>(builder.Configuration.GetSection(AppointmentOptions.SectionName));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddScoped<IPetPhotoStorage, PetPhotoStorage>();

// ---------- ASP.NET Core Identity ----------
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Parola politikası (kayıt olan herkes için). Seed'deki admin parolası "sau" bunun dışındadır (DbSeeder).
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        // Kaba kuvvet (brute force) saldırısına karşı: 5 hatalı denemede 5 dakika kilit.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;

        // Veritabanı şemasını, ilk migration'ın oluşturulduğu Identity şemasıyla aynı tut.
        options.Stores.SchemaVersion = IdentitySchemaVersions.Version1;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<TurkishIdentityErrorDescriber>()
    .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "PatiVet.Auth";
    options.Cookie.HttpOnly = true;                              // JavaScript çereze erişemez (XSS'e karşı)
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;                  // Başka sitelerden gelen POST'larda çerez gönderilmez
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;

    // API ve MCP isteklerinde giriş sayfasına yönlendirme (302) yerine 401/403 dönülür.
    options.Events.OnRedirectToLogin = context =>
    {
        if (IsApiRequest(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (IsApiRequest(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

// Kilitlenen kullanıcının açık oturumu en geç 5 dakika içinde sonlansın.
builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.FromMinutes(5));

// ---------- REST API güvenliği: JWT + yetkilendirme politikaları (API/DependencyInjection.cs) ----------
builder.Services.AddVeterinerKlinigiApiSecurity(builder.Configuration, builder.Environment);

// ---------- MVC + API controller'ları ----------
builder.Services
    .AddControllersWithViews(options =>
    {
        // Tüm MVC POST isteklerinde anti-forgery token kontrolü (CSRF koruması).
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());

        // Zorunluluk sadece [Required] ile belirtilsin (C# nullable ayarı tek başına "zorunlu" anlamına gelmesin).
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

        // Model binding hata mesajlarını Türkçeleştir.
        var messages = options.ModelBindingMessageProvider;
        messages.SetValueMustNotBeNullAccessor(_ => "Bu alan zorunludur.");
        messages.SetMissingBindRequiredValueAccessor(field => $"{field} alanı zorunludur.");
        messages.SetMissingKeyOrValueAccessor(() => "Değer zorunludur.");
        messages.SetMissingRequestBodyRequiredValueAccessor(() => "İstek gövdesi boş olamaz.");
        messages.SetAttemptedValueIsInvalidAccessor((value, field) => $"'{value}' değeri {field} için geçerli değil.");
        messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"'{value}' değeri geçerli değil.");
        messages.SetUnknownValueIsInvalidAccessor(field => $"{field} için girilen değer geçerli değil.");
        messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "Girilen değer geçerli değil.");
        messages.SetValueIsInvalidAccessor(value => $"'{value}' değeri geçerli değil.");
        messages.SetValueMustBeANumberAccessor(field => $"{field} alanı bir sayı olmalıdır.");
        messages.SetNonPropertyValueMustBeANumberAccessor(() => "Bu alan bir sayı olmalıdır.");
    })
    .AddJsonOptions(options =>
    {
        // API cevaplarında enum'lar sayı yerine metin olsun: "status": "Pending"
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    })
    .AddVeterinerKlinigiApi(); // API projesindeki [ApiController] sınıflarını dahil eder

// API hata cevapları için standart ProblemDetails biçimi ve OpenAPI dokümanı (/openapi/v1.json).
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// ---------- MCP sunucusu (Model Context Protocol) ----------
builder.Services
    .AddMcpServer(options =>
    {
        options.ServerInfo = new() { Name = "PatiVet Veteriner Kliniği", Version = "1.0.0" };
    })
    // Stateless: her istek bağımsızdır; HttpContext (ve giriş yapan kullanıcı) araçlarda kullanılabilir.
    .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<VeterinerKlinigiTools>();

// Başlangıç verisi ayarları (admin hesabı, örnek veriler).
var seedOptions = builder.Configuration.GetSection(SeedOptions.SectionName).Get<SeedOptions>() ?? new SeedOptions();

var app = builder.Build();

// ---------------------------------------------------------------------
// 2) HTTP İSTEK HATTI (Middleware pipeline) — SIRA ÖNEMLİDİR
// ---------------------------------------------------------------------

if (app.Environment.IsDevelopment())
{
    // Geliştirmede hata detayı gösterilir; OpenAPI dokümanı yayınlanır.
    app.MapOpenApi();
}
else
{
    // Canlı ortamda teknik detay gösterilmez: API için ProblemDetails, sayfalar için hata sayfası.
    app.UseWhen(context => IsApiRequest(context.Request), api => api.UseExceptionHandler());
    app.UseWhen(context => !IsApiRequest(context.Request), web => web.UseExceptionHandler("/Home/Error"));
    app.UseHsts();
}

// 404, 403 gibi durum kodları için kullanıcı dostu sayfa (API ve MCP hariç).
app.UseWhen(context => !IsApiRequest(context.Request),
    web => web.UseStatusCodePagesWithReExecute("/Home/HttpStatus", "?code={0}"));

// Temel güvenlik başlıkları.
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";       // Tarayıcı dosya tipini tahmin etmesin
    context.Response.Headers["X-Frame-Options"] = "DENY";                 // Site başka bir sitede iframe içinde açılamasın (clickjacking)
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseHttpsRedirection();

// Türkçe kültür: tarih (23.11.2026), para (1.250,00 ₺) ve ondalık (4,5) biçimleri.
var turkish = new CultureInfo("tr-TR");
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(turkish),
    SupportedCultures = [turkish],
    SupportedUICultures = [turkish]
});

// Kullanıcının yüklediği fotoğraflar (wwwroot/uploads) derleme sırasında bilinmediği için
// MapStaticAssets'e ek olarak klasik statik dosya middleware'i de gereklidir.
app.UseStaticFiles();

app.UseRouting();

// Önce "kimsin?" (kimlik doğrulama), sonra "yetkin var mı?" (yetkilendirme).
app.UseAuthentication();
app.UseAuthorization();

// wwwroot altındaki derleme zamanı dosyaları (css, js, lib, img) sıkıştırılmış ve önbelleğe uygun sunulur.
app.MapStaticAssets();

// Area rotası: /Admin/Clinics/Edit/5 → Area=Admin, Controller=Clinics, Action=Edit, id=5
// {area:exists} kısıtı, sadece projede gerçekten var olan Area adlarını eşleştirir
// (Admin, Veterinarian, Member). Area rotası her zaman varsayılan rotadan önce yazılır.
app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();

// Varsayılan rota: /Home/Index, /Account/Login ...
app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// [Route("api/...")] ile işaretlenmiş API controller'larının uçları.
app.MapControllers();

// MCP ucu: /mcp — sadece geçerli bir JWT ile erişilebilir.
app.MapMcp("/mcp").RequireAuthorization(ApiPolicies.Mcp);

// Veritabanını hazırla: migration'ları uygula, rolleri/admin'i/örnek verileri ekle.
await app.Services.InitializeDatabaseAsync(seedOptions);

if (app.Services.GetRequiredService<IOptions<JwtOptions>>().Value.IsEphemeralKey)
{
    app.Logger.LogWarning(
        "Jwt:Key tanımlı değil; geçici bir anahtar üretildi. Uygulama yeniden başlatılınca JWT'ler geçersiz olur. " +
        "Kalıcı anahtar için: dotnet user-secrets set \"Jwt:Key\" \"<en az 32 karakter>\" --project src/VeterinerKlinigi.Web");
}

await app.RunAsync();

// İstek REST API'ye veya MCP ucuna mı geliyor? (Bu isteklere HTML sayfası değil, JSON/durum kodu döneriz.)
static bool IsApiRequest(HttpRequest request)
    => request.Path.StartsWithSegments("/api") || request.Path.StartsWithSegments("/mcp");
