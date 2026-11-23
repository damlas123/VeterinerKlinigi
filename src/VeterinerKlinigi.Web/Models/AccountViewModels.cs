using System.ComponentModel.DataAnnotations;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Web.Models;

public class LoginViewModel
{
    [Display(Name = "E-posta")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EmailAddress(ErrorMessage = ValidationMessages.Email)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Parola")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Beni hatırla")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Display(Name = "Ad")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.PersonName, ErrorMessage = ValidationMessages.MaxLength)]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Soyad")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.PersonName, ErrorMessage = ValidationMessages.MaxLength)]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EmailAddress(ErrorMessage = ValidationMessages.Email)]
    [StringLength(FieldLengths.Email, ErrorMessage = ValidationMessages.MaxLength)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Telefon")]
    [Phone(ErrorMessage = ValidationMessages.Phone)]
    [StringLength(FieldLengths.Phone, ErrorMessage = ValidationMessages.MaxLength)]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Parola")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = ValidationMessages.LengthRange)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Parola (tekrar)")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Parolalar birbiriyle aynı olmalıdır.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Kişisel verilerimin randevu hizmeti için işlenmesini kabul ediyorum")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Devam etmek için onay vermelisiniz.")]
    public bool AcceptTerms { get; set; }
}

public class ProfileViewModel
{
    [Display(Name = "Ad")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.PersonName, ErrorMessage = ValidationMessages.MaxLength)]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Soyad")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.PersonName, ErrorMessage = ValidationMessages.MaxLength)]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "Telefon")]
    [Phone(ErrorMessage = ValidationMessages.Phone)]
    [StringLength(FieldLengths.Phone, ErrorMessage = ValidationMessages.MaxLength)]
    public string? PhoneNumber { get; set; }

    /// <summary>Sadece gösterim (e-posta giriş adı olduğu için değiştirilemez).</summary>
    public string Email { get; set; } = string.Empty;

    public IList<string> Roles { get; set; } = [];

    public DateTime CreatedAt { get; set; }
}

public class ChangePasswordViewModel
{
    [Display(Name = "Mevcut parola")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Display(Name = "Yeni parola")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = ValidationMessages.LengthRange)]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Display(Name = "Yeni parola (tekrar)")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Parolalar birbiriyle aynı olmalıdır.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ApiTokenViewModel
{
    public string? Token { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public bool IsEphemeralKey { get; set; }

    public string BaseUrl { get; set; } = string.Empty;
}
