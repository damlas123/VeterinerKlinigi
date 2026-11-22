namespace VeterinerKlinigi.Entities;

/// <summary>
/// Veteriner ile Hizmet arasındaki çoka-çok ilişkinin ara tablosu.
/// "Hangi hekim hangi hizmeti verebilir?" sorusunun cevabıdır.
/// Birincil anahtar (VeterinarianId, ServiceId) ikilisidir; aynı eşleşme iki kez eklenemez.
/// </summary>
public class VeterinarianService
{
    public int VeterinarianId { get; set; }

    public int ServiceId { get; set; }

    public Veterinarian Veterinarian { get; set; } = null!;

    public Service Service { get; set; } = null!;
}
