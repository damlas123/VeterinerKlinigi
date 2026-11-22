namespace VeterinerKlinigi.Entities;

/// <summary>
/// Kliniğin sunduğu hizmet: muayene, aşı, kısırlaştırma, diş temizliği, tıraş ve bakım vb.
/// </summary>
public class Service
{
    public int Id { get; set; }

    public int ClinicId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>Hizmet süresi (DAKİKA). Randevu çakışma hesabında kullanılır.</summary>
    public int Duration { get; set; }

    /// <summary>Hizmet ücreti (TL).</summary>
    public decimal Price { get; set; }

    /// <summary>false ise hizmet arşivlenmiştir; yeni randevuda listelenmez.</summary>
    public bool IsActive { get; set; } = true;

    public Clinic Clinic { get; set; } = null!;

    /// <summary>Bu hizmeti verebilen hekimler (çoka-çok ara tablo).</summary>
    public ICollection<VeterinarianService> VeterinarianServices { get; set; } = new List<VeterinarianService>();

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
