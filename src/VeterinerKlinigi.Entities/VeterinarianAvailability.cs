namespace VeterinerKlinigi.Entities;

/// <summary>
/// Hekimin haftalık müsaitlik aralığı. Örn: Pazartesi 09:00 - 12:00.
/// Aynı güne birden fazla aralık girilebilir (öğle arası için 09:00-12:00 ve 13:00-17:00).
/// Randevular yalnızca bu aralıkların içine alınabilir.
/// </summary>
public class VeterinarianAvailability
{
    public int Id { get; set; }

    public int VeterinarianId { get; set; }

    /// <summary>Haftanın günü (.NET'in DayOfWeek enum'u: 0 = Pazar, 1 = Pazartesi ... 6 = Cumartesi).</summary>
    public DayOfWeek DayOfWeek { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public Veterinarian Veterinarian { get; set; } = null!;
}
