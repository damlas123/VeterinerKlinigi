using System.ComponentModel.DataAnnotations;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Business.Dtos;

/// <summary>Kliniğin sunduğu hizmet.</summary>
public record ServiceDto(
    int Id,
    int ClinicId,
    string ClinicName,
    string Name,
    string? Description,
    int Duration,
    decimal Price,
    bool IsActive,
    int VeterinarianCount);

/// <summary>Hizmetin kısa özeti (hekim kartlarında, açılır listelerde).</summary>
public record ServiceBriefDto(int Id, string Name, int Duration, decimal Price);

/// <summary>Hizmet ekleme / güncelleme formu.</summary>
public class ServiceSaveDto
{
    [Display(Name = "Klinik")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir klinik seçin.")]
    public int ClinicId { get; set; }

    [Display(Name = "Hizmet adı")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.ServiceName, ErrorMessage = ValidationMessages.MaxLength)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Açıklama")]
    [StringLength(FieldLengths.ServiceDescription, ErrorMessage = ValidationMessages.MaxLength)]
    public string? Description { get; set; }

    [Display(Name = "Süre (dakika)")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [Range(5, 480, ErrorMessage = ValidationMessages.Range)]
    public int Duration { get; set; } = 30;

    // ParseLimitsInInvariantCulture: "0.01" gibi sınırlar Türkçe kültürde (virgül) yanlış okunmasın.
    [Display(Name = "Ücret (TL)")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [Range(typeof(decimal), "0", "1000000", ErrorMessage = ValidationMessages.Range, ParseLimitsInInvariantCulture = true)]
    public decimal Price { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
