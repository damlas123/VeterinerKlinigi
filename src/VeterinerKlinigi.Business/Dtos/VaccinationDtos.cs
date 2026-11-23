using System.ComponentModel.DataAnnotations;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Business.Dtos;

/// <summary>Bir sonraki doz tarihine göre aşının durumu.</summary>
public enum VaccinationDueState
{
    /// <summary>Sonraki doz tarihi girilmemiş veya daha yeni bir dozla tamamlanmış.</summary>
    None = 0,

    /// <summary>Önümüzdeki 30 gün içinde.</summary>
    Upcoming = 1,

    /// <summary>Tarihi geçmiş, henüz yapılmamış.</summary>
    Overdue = 2,

    /// <summary>30 günden daha ileride.</summary>
    Planned = 3
}

/// <summary>Aşı kaydı.</summary>
public record VaccinationDto(
    int Id,
    int PetId,
    string PetName,
    string VaccineName,
    DateOnly VaccinationDate,
    DateOnly? NextDoseDate,
    string? Description,
    int? VeterinarianId,
    string? VeterinarianName)
{
    /// <summary>Bu aşının sonraki doz durumu (servis bugünün tarihine göre doldurur).</summary>
    public VaccinationDueState DueState { get; init; }

    /// <summary>Sonraki doza kalan gün (geçmişse negatif).</summary>
    public int? DaysUntilNextDose { get; init; }

    public string DueStateText => DueState switch
    {
        VaccinationDueState.Upcoming => DaysUntilNextDose == 0 ? "Bugün" : $"{DaysUntilNextDose} gün kaldı",
        VaccinationDueState.Overdue => $"{-DaysUntilNextDose} gün gecikti",
        VaccinationDueState.Planned => "Planlandı",
        _ => "-"
    };
}

/// <summary>Bir evcil hayvanın aşı takvimi: geçmiş, yaklaşan ve geciken aşılar.</summary>
public record VaccinationScheduleDto(
    int PetId,
    string PetName,
    string SpeciesName,
    IReadOnlyList<VaccinationDto> History,
    IReadOnlyList<VaccinationDto> Upcoming,
    IReadOnlyList<VaccinationDto> Overdue);

/// <summary>Aşı kaydı ekleme / güncelleme formu.</summary>
public class VaccinationSaveDto
{
    [Display(Name = "Evcil hayvan")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir evcil hayvan seçin.")]
    public int PetId { get; set; }

    [Display(Name = "Aşı adı")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.VaccineName, ErrorMessage = ValidationMessages.MaxLength)]
    public string VaccineName { get; set; } = string.Empty;

    [Display(Name = "Uygulama tarihi")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    public DateOnly VaccinationDate { get; set; }

    [Display(Name = "Sonraki doz tarihi")]
    public DateOnly? NextDoseDate { get; set; }

    [Display(Name = "Açıklama")]
    [StringLength(FieldLengths.VaccinationDescription, ErrorMessage = ValidationMessages.MaxLength)]
    public string? Description { get; set; }

    /// <summary>Uygulayan hekim. Veteriner kaydederken otomatik olarak kendisi atanır.</summary>
    [Display(Name = "Uygulayan hekim")]
    public int? VeterinarianId { get; set; }
}
