using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using VeterinerKlinigi.API.Auth;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.API;

/// <summary>
/// API katmanının dışarıya açtığı kayıt metotları.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Bu projedeki (VeterinerKlinigi.API) controller'ları MVC'ye tanıtır.
    /// ASP.NET Core varsayılan olarak controller'ları sadece çalışan projede (Web) arar;
    /// AddApplicationPart ile "bu derlemedeki controller'ları da kullan" demiş oluruz.
    /// </summary>
    public static IMvcBuilder AddVeterinerKlinigiApi(this IMvcBuilder mvcBuilder)
    {
        mvcBuilder.AddApplicationPart(typeof(DependencyInjection).Assembly);
        return mvcBuilder;
    }

    /// <summary>
    /// REST API ve MCP güvenliği: JWT Bearer kimlik doğrulaması ve yetkilendirme politikaları.
    /// AddIdentity'den SONRA çağrılmalıdır (Identity çerez şemasını ve varsayılanları ayarlar).
    /// </summary>
    public static IServiceCollection AddVeterinerKlinigiApiSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        if (string.IsNullOrWhiteSpace(jwt.Key))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Jwt:Key ayarı bulunamadı. Anahtarı user-secrets veya ortam değişkeni ile tanımlayın (en az 32 karakter).");
            }

            // Geliştirme ortamında anahtar tanımlanmamışsa uygulama yine çalışsın diye rastgele bir
            // anahtar üretilir. Dezavantajı: uygulama her yeniden başladığında eski token'lar geçersiz olur.
            jwt.Key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            jwt.IsEphemeralKey = true;
        }

        if (Encoding.UTF8.GetByteCount(jwt.Key) < 32)
        {
            throw new InvalidOperationException("Jwt:Key en az 32 karakter olmalıdır (HMAC-SHA256 için 256 bit).");
        }

        services.AddSingleton<IOptions<JwtOptions>>(Options.Create(jwt));
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Identity zaten çerez şemasını ekledi; burada ek olarak JWT şemasını ekliyoruz.
        services.AddAuthentication()
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Claim adlarını olduğu gibi kullan (çerezdeki claim adlarıyla aynı kalsın).
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(ApiPolicies.ReadAccess, policy => policy
                .AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());

            options.AddPolicy(ApiPolicies.WriteAccess, policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());

            options.AddPolicy(ApiPolicies.AdminRead, policy => policy
                .AddAuthenticationSchemes(IdentityConstants.ApplicationScheme, JwtBearerDefaults.AuthenticationScheme)
                .RequireRole(RoleNames.Admin));

            options.AddPolicy(ApiPolicies.Mcp, policy => policy
                .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());
        });

        return services;
    }
}
