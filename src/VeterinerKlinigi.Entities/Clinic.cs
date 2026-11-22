namespace VeterinerKlinigi.Entities;

/// <summary>
/// Veteriner kliniği. Sistem birden fazla kliniği destekler.
/// </summary>
public class Clinic
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? Description { get; set; }

    /// <summary>Kliniğin açılış saati. Hekim müsaitlikleri bu saatten önce başlayamaz.</summary>
    public TimeOnly OpeningTime { get; set; }

    /// <summary>Kliniğin kapanış saati. Hekim müsaitlikleri bu saatten sonra bitemez.</summary>
    public TimeOnly ClosingTime { get; set; }

    /// <summary>false ise klinik arşivlenmiştir (silinmez, listelenmez).</summary>
    public bool IsActive { get; set; } = true;

    public ICollection<Veterinarian> Veterinarians { get; set; } = new List<Veterinarian>();

    public ICollection<Service> Services { get; set; } = new List<Service>();
}
