namespace VeterinerKlinigi.API.Auth;

/// <summary>
/// REST API ve MCP için yetkilendirme politikalarının adları.
/// </summary>
public static class ApiPolicies
{
    /// <summary>
    /// Okuma (GET): Tarayıcı oturum çerezi VEYA JWT kabul edilir.
    /// Böylece MVC sayfalarındaki jQuery kodu da (çerezle) API'yi çağırabilir.
    /// </summary>
    public const string ReadAccess = "ApiRead";

    /// <summary>
    /// Yazma (POST): SADECE JWT kabul edilir. Tarayıcı JWT'yi kendiliğinden göndermediği için
    /// başka bir sitenin kullanıcı adına istek göndermesi (CSRF) mümkün olmaz.
    /// </summary>
    public const string WriteAccess = "ApiWrite";

    /// <summary>Raporlar: çerez veya JWT, ve Admin rolü.</summary>
    public const string AdminRead = "ApiAdminRead";

    /// <summary>MCP ucu (/mcp): JWT ile giriş yapmış herhangi bir kullanıcı.</summary>
    public const string Mcp = "Mcp";
}
