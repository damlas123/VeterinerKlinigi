using System.ComponentModel.DataAnnotations;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Business.Dtos;

/// <summary>Veteriner hekim bilgisi.</summary>
public record VeterinarianDto(
    int Id,
    string FullName,
    string Specialty,
    string? Biography,
    int ClinicId,
    string ClinicName,
    string Email,
    string? PhoneNumber,
    bool IsActive,
    List<ServiceBriefDto> Services);

/// <summary>Veteriner detayı: bilgiler + haftalık çalışma saatleri.</summary>
public record VeterinarianDetailDto(VeterinarianDto Veterinarian, IReadOnlyList<AvailabilityDto> Availabilities);

/// <summary>Veteriner listesini filtrelemek için (LINQ ile uygulanır).</summary>
public class VeterinarianFilter
{
    public int? ClinicId { get; set; }

    public int? ServiceId { get; set; }

    /// <summary>Uzmanlık alanında geçen metin (ör. "cerrahi").</summary>
    public string? Specialty { get; set; }

    public bool IncludeInactive { get; set; }
}

/// <summary>Yeni veteriner: hem giriş hesabı hem de hekim kaydı oluşturulur.</summary>
public class VeterinarianCreateDto
{
    [Display(Name = "Ad")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.PersonName, ErrorMessage = ValidationMessages.MaxLength)]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Soyad")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.PersonName, ErrorMessage = ValidationMessages.MaxLength)]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "E-posta (giriş adı)")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EmailAddress(ErrorMessage = ValidationMessages.Email)]
    [StringLength(FieldLengths.Email, ErrorMessage = ValidationMessages.MaxLength)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Telefon")]
    [Phone(ErrorMessage = ValidationMessages.Phone)]
    [StringLength(FieldLengths.Phone, ErrorMessage = ValidationMessages.MaxLength)]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Geçici parola")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(100, MinimumLength = 8, ErrorMessage = ValidationMessages.LengthRange)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Görünen ad (unvanıyla)")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.FullName, ErrorMessage = ValidationMessages.MaxLength)]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Uzmanlık alanı")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.Specialty, ErrorMessage = ValidationMessages.MaxLength)]
    public string Specialty { get; set; } = string.Empty;

    [Display(Name = "Biyografi")]
    [StringLength(FieldLengths.Biography, ErrorMessage = ValidationMessages.MaxLength)]
    public string? Biography { get; set; }

    [Display(Name = "Klinik")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir klinik seçin.")]
    public int ClinicId { get; set; }

    [Display(Name = "Verebildiği hizmetler")]
    public List<int> ServiceIds { get; set; } = [];
}

/// <summary>Veteriner güncelleme (giriş e-postası ve parolası burada değişmez).</summary>
public class VeterinarianUpdateDto
{
    [Display(Name = "Görünen ad (unvanıyla)")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.FullName, ErrorMessage = ValidationMessages.MaxLength)]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Uzmanlık alanı")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.Specialty, ErrorMessage = ValidationMessages.MaxLength)]
    public string Specialty { get; set; } = string.Empty;

    [Display(Name = "Biyografi")]
    [StringLength(FieldLengths.Biography, ErrorMessage = ValidationMessages.MaxLength)]
    public string? Biography { get; set; }

    [Display(Name = "Telefon")]
    [Phone(ErrorMessage = ValidationMessages.Phone)]
    [StringLength(FieldLengths.Phone, ErrorMessage = ValidationMessages.MaxLength)]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Klinik")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir klinik seçin.")]
    public int ClinicId { get; set; }

    [Display(Name = "Verebildiği hizmetler")]
    public List<int> ServiceIds { get; set; } = [];

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
