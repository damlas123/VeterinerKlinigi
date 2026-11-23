using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Business.Abstractions;

/// <summary>
/// İsteği yapan (giriş yapmış) kullanıcının bilgileri.
/// Business katmanı HTTP'yi bilmez; bu arayüzün uygulaması Web projesindedir
/// (HttpContext.User'dan okur). MVC sayfası, REST API ve MCP aracı aynı uygulamayı kullanır;
/// böylece sahiplik kontrolleri her kanalda aynı şekilde çalışır.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>Kullanıcının Id'si (AspNetUsers.Id). Giriş yapılmamışsa null.</summary>
    string? UserId { get; }

    /// <summary>Kullanıcı bir veteriner ise Veterinarians.Id değeri.</summary>
    int? VeterinarianId { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);

    bool IsAdmin => IsInRole(RoleNames.Admin);

    bool IsVeterinarian => IsInRole(RoleNames.Veterinarian);

    bool IsMember => IsInRole(RoleNames.Member);
}
