using System.ComponentModel.DataAnnotations;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Entities.Constants;
using VeterinerKlinigi.Entities.Enums;

namespace VeterinerKlinigi.Business.Dtos;

/// <summary>Evcil hayvan bilgisi.</summary>
public record PetDto(
    int Id,
    string Name,
    PetSpecies Species,
    string? Breed,
    DateOnly BirthDate,
    decimal Weight,
    string? PhotoUrl,
    bool IsActive,
    string OwnerId,
    string OwnerName)
{
    /// <summary>Türün Türkçe adı ("Kedi").</summary>
    public string SpeciesName => DisplayText.Species(Species);
}

/// <summary>Evcil hayvanın sağlık geçmişi: bilgiler, aşılar, randevular ve muayene notları.</summary>
public record PetDetailDto(
    PetDto Pet,
    string OwnerEmail,
    string? OwnerPhone,
    IReadOnlyList<VaccinationDto> Vaccinations,
    IReadOnlyList<AppointmentDto> Appointments,
    IReadOnlyList<ExaminationNoteDto> ExaminationNotes,
    bool CanEdit,
    bool CanManageVaccinations);

/// <summary>Evcil hayvan listesini filtrelemek için.</summary>
public class PetFilter
{
    /// <summary>Ad, ırk veya sahip adında arama.</summary>
    public string? Search { get; set; }

    /// <summary>Belirli bir sahibin hayvanları (Admin için).</summary>
    public string? OwnerId { get; set; }

    /// <summary>Arşivlenmiş hayvanlar da listelensin mi?</summary>
    public bool IncludeInactive { get; set; }
}

/// <summary>Evcil hayvan ekleme / güncelleme formu (fotoğraf Web katmanında ayrıca işlenir).</summary>
public class PetSaveDto
{
    [Display(Name = "Adı")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.PetName, ErrorMessage = ValidationMessages.MaxLength)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Türü")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [EnumDataType(typeof(PetSpecies), ErrorMessage = "Geçerli bir tür seçin.")]
    public PetSpecies Species { get; set; } = PetSpecies.Cat;

    [Display(Name = "Irkı")]
    [StringLength(FieldLengths.Breed, ErrorMessage = ValidationMessages.MaxLength)]
    public string? Breed { get; set; }

    [Display(Name = "Doğum tarihi")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    public DateOnly BirthDate { get; set; }

    [Display(Name = "Kilo (kg)")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [Range(typeof(decimal), "0.01", "150", ErrorMessage = ValidationMessages.Range, ParseLimitsInInvariantCulture = true)]
    public decimal Weight { get; set; }
}

/// <summary>Düzenleme formu için: form verisi + mevcut fotoğraf.</summary>
public record PetEditDto(PetSaveDto Form, string? PhotoUrl);

/// <summary>Evcil hayvan silme sonucu. Silindiyse fotoğrafı da diskten kaldırılmalıdır.</summary>
public record PetDeleteResult(DeleteOutcome Outcome, string? PhotoUrl);
