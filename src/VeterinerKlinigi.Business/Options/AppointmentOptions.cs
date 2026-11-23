namespace VeterinerKlinigi.Business.Options;

/// <summary>
/// Randevu kurallarının ayarlanabilir değerleri. appsettings.json → "Appointment" bölümü.
/// </summary>
public class AppointmentOptions
{
    public const string SectionName = "Appointment";

    /// <summary>Randevu başlangıç saatlerinin aralığı (dakika). 15 → 10:00, 10:15, 10:30 ...</summary>
    public int SlotIntervalMinutes { get; set; } = 15;

    /// <summary>En fazla kaç gün sonrasına randevu alınabilir.</summary>
    public int MaxDaysAhead { get; set; } = 60;
}
