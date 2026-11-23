namespace VeterinerKlinigi.API.Auth;

/// <summary>
/// JWT ayarları (appsettings.json → "Jwt").
/// İmzalama anahtarı (Key) GİZLİDİR ve depoya konmaz; "dotnet user-secrets" ile tanımlanır:
///   dotnet user-secrets set "Jwt:Key" "en-az-32-karakterlik-rastgele-bir-anahtar" --project src/VeterinerKlinigi.Web
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>Token'ı üreten (bu uygulama).</summary>
    public string Issuer { get; set; } = "VeterinerKlinigi";

    /// <summary>Token'ı kullanacak istemciler (Postman, MCP Inspector ...).</summary>
    public string Audience { get; set; } = "VeterinerKlinigi.Clients";

    /// <summary>HMAC-SHA256 imzalama anahtarı (en az 32 karakter).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Token geçerlilik süresi (dakika).</summary>
    public int ExpiryMinutes { get; set; } = 120;

    /// <summary>
    /// true ise anahtar ayarlarda bulunamadığı için uygulama açılırken rastgele üretilmiştir
    /// (sadece Development). Uygulama yeniden başlatılınca eski token'lar geçersiz olur.
    /// </summary>
    public bool IsEphemeralKey { get; set; }
}
