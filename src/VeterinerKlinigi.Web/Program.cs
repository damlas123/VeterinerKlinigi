using VeterinerKlinigi.API;
using VeterinerKlinigi.Business;
using VeterinerKlinigi.Data;

// =====================================================================
// Program.cs — Uygulamanın giriş noktası ve "kompozisyon kökü".
// Tüm katmanlar (Business, API, Data, Identity, MCP) burada
// birbirine bağlanır. İki bölümden oluşur:
//   1) builder.Services  → Dependency Injection (DI) kayıtları
//   2) app.Use... / Map... → HTTP isteğinin geçtiği middleware hattı
// =====================================================================

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------
// 1) SERVİS KAYITLARI (Dependency Injection)
// ---------------------------------------------------------------------

// Veri katmanı: AppDbContext'i SQL Server bağlantısıyla kaydeder (Data/DependencyInjection.cs).
// Bağlantı dizesi appsettings.json → "ConnectionStrings:DefaultConnection" ayarından okunur.
// Aynı anahtar user-secrets'ta tanımlıysa o değer önceliklidir (farklı SQL Server kullananlar için).
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Veritabanı bağlantı dizesi bulunamadı. appsettings.json içinde 'ConnectionStrings:DefaultConnection' tanımlayın.");
builder.Services.AddDataLayer(connectionString);

// İş katmanının servisleri (Business/DependencyInjection.cs).
// Web projesi Business'ın iç yapısını bilmez; sadece bu tek metodu çağırır.
builder.Services.AddBusinessLayer();

// MVC: Controller + Razor View desteği.
// AddVeterinerKlinigiApi(): VeterinerKlinigi.API projesindeki
// [ApiController] sınıflarını bu uygulamaya dahil eder.
builder.Services
    .AddControllersWithViews()
    .AddVeterinerKlinigiApi();

var app = builder.Build();

// ---------------------------------------------------------------------
// 2) HTTP İSTEK HATTI (Middleware pipeline) — SIRA ÖNEMLİDİR
// ---------------------------------------------------------------------

if (!app.Environment.IsDevelopment())
{
    // Canlı ortamda hata detayı gösterme, kullanıcı dostu hata sayfasına yönlendir.
    app.UseExceptionHandler("/Home/Error");
    // Tarayıcıya "bu siteye sadece HTTPS ile gel" bilgisini gönderir.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// Yetkilendirme: [Authorize] ile işaretli controller/action'ları korur.
// Kimlik doğrulama (UseAuthentication) her zaman bundan ÖNCE gelmelidir.
app.UseAuthorization();

// wwwroot altındaki dosyaları (css, js, lib, img) sıkıştırılmış ve
// önbelleğe uygun şekilde sunar (.NET 9+ ile gelen yöntem).
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

app.Run();
