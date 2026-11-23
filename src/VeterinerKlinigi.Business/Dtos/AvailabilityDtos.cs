using System.ComponentModel.DataAnnotations;
using VeterinerKlinigi.Business.Common;

namespace VeterinerKlinigi.Business.Dtos;

/// <summary>Hekimin haftalık müsaitlik aralığı.</summary>
public record AvailabilityDto(int Id, int VeterinarianId, DayOfWeek DayOfWeek, TimeOnly StartTime, TimeOnly EndTime)
{
    /// <summary>Türkçe gün adı ("Pazartesi").</summary>
    public string DayName => DisplayText.Day(DayOfWeek);
}

/// <summary>Müsaitlik ekleme / güncelleme formu.</summary>
public class AvailabilitySaveDto
{
    /// <summary>Veteriner kendi kaydını eklerken bu alan servis tarafından kendi Id'siyle değiştirilir.</summary>
    [Display(Name = "Veteriner")]
    public int VeterinarianId { get; set; }

    [Display(Name = "Gün")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Monday;

    [Display(Name = "Başlangıç saati")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    public TimeOnly StartTime { get; set; } = new(9, 0);

    [Display(Name = "Bitiş saati")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    public TimeOnly EndTime { get; set; } = new(17, 0);
}
