using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.API.Contracts;

/// <summary>POST /api/auth/token isteği.</summary>
public class TokenRequest
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi girin.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parola zorunludur.")]
    public string Password { get; set; } = string.Empty;
}

/// <summary>Üretilen JWT. İsteklerde "Authorization: Bearer {AccessToken}" başlığıyla gönderilir.</summary>
public record TokenResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    string Email,
    IReadOnlyList<string> Roles);

/// <summary>GET /api/auth/me cevabı: token'ın kime ait olduğu.</summary>
public record CurrentUserResponse(
    string UserId,
    string? FullName,
    string? Email,
    IReadOnlyList<string> Roles,
    int? VeterinarianId);
