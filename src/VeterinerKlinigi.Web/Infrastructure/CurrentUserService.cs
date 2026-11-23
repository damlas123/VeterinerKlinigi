using System.Security.Claims;
using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Business.Common;

namespace VeterinerKlinigi.Web.Infrastructure;

/// <summary>
/// ICurrentUserService'in Web uygulaması: kullanıcı bilgisini HttpContext.User'dan okur.
/// Kullanıcı ister tarayıcı çereziyle, ister JWT ile (API / MCP) gelsin, claim'ler aynıdır;
/// bu yüzden Business katmanındaki sahiplik kontrolleri her kanalda aynı çalışır.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public int? VeterinarianId =>
        int.TryParse(User?.FindFirstValue(AppClaimTypes.VeterinarianId), out var id) ? id : null;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role) => User?.IsInRole(role) == true;
}
