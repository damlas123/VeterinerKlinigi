namespace VeterinerKlinigi.Entities;

/// <summary>
/// Muayene notu. Hekim, randevuyu tamamlarken tanı ve tedavi bilgisini girer.
/// Bir randevunun en fazla bir muayene notu olabilir.
/// </summary>
public class ExaminationNote
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }

    /// <summary>Notu yazan hekim.</summary>
    public int VeterinarianId { get; set; }

    /// <summary>Tanı.</summary>
    public string Diagnosis { get; set; } = string.Empty;

    /// <summary>Uygulanan / önerilen tedavi.</summary>
    public string Treatment { get; set; } = string.Empty;

    /// <summary>Ek notlar (opsiyonel).</summary>
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }

    public Appointment Appointment { get; set; } = null!;

    public Veterinarian Veterinarian { get; set; } = null!;
}
