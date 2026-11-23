using System.ComponentModel.DataAnnotations;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Business.Dtos;

/// <summary>Klinik bilgisi (listeleme ve detay için).</summary>
public record ClinicDto(
    int Id,
    string Name,
    string Address,
    string Phone,
    string? Email,
    string? Description,
    TimeOnly OpeningTime,
    TimeOnly ClosingTime,
    bool IsActive,
    int VeterinarianCount,
    int ServiceCount);

/// <summary>Klinik ekleme / güncelleme formu. MVC formu ve servis aynı modeli kullanır.</summary>
public class ClinicSaveDto
{
    [Display(Name = "Klinik adı")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.ClinicName, ErrorMessage = ValidationMessages.MaxLength)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Adres")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.Address, ErrorMessage = ValidationMessages.MaxLength)]
    public string Address { get; set; } = string.Empty;

    [Display(Name = "Telefon")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [Phone(ErrorMessage = ValidationMessages.Phone)]
    [StringLength(FieldLengths.Phone, ErrorMessage = ValidationMessages.MaxLength)]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    [EmailAddress(ErrorMessage = ValidationMessages.Email)]
    [StringLength(FieldLengths.Email, ErrorMessage = ValidationMessages.MaxLength)]
    public string? Email { get; set; }

    [Display(Name = "Açıklama")]
    [StringLength(FieldLengths.ClinicDescription, ErrorMessage = ValidationMessages.MaxLength)]
    public string? Description { get; set; }

    [Display(Name = "Açılış saati")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    public TimeOnly OpeningTime { get; set; } = new(9, 0);

    [Display(Name = "Kapanış saati")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    public TimeOnly ClosingTime { get; set; } = new(18, 0);

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
