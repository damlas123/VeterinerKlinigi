using VeterinerKlinigi.Entities.Enums;

namespace VeterinerKlinigi.Entities;

/// <summary>
/// Randevu. Hayvan sahibi; evcil hayvanını, hizmeti, hekimi ve saati seçerek oluşturur.
/// </summary>
public class Appointment
{
    public int Id { get; set; }

    public int PetId { get; set; }

    /// <summary>
    /// Randevunun sahibi (AspNetUsers.Id). Hayvanın sahibinden kopyalanır.
    /// "Randevularım" sorgularını ve sahiplik kontrolünü hızlandırır.
    /// </summary>
    public string OwnerId { get; set; } = string.Empty;

    public int VeterinarianId { get; set; }

    public int ServiceId { get; set; }

    /// <summary>Randevunun BAŞLANGIÇ tarihi ve saati (klinik yerel saati).</summary>
    public DateTime AppointmentDate { get; set; }

    /// <summary>
    /// Süre (dakika). Randevu oluşturulurken hizmetten KOPYALANIR; hizmetin süresi
    /// sonradan değişse bile bu randevunun süresi değişmez.
    /// </summary>
    public int Duration { get; set; }

    /// <summary>
    /// Ücret (TL). Randevu oluşturulurken hizmetten KOPYALANIR; fiyat sonradan
    /// değişse bile geçmiş randevuların ücreti ve ciro raporu doğru kalır.
    /// </summary>
    public decimal Price { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    /// <summary>Hayvan sahibinin notu / şikâyeti (opsiyonel).</summary>
    public string? OwnerNote { get; set; }

    /// <summary>Ret gerekçesi. Sadece Rejected durumunda doldurulur.</summary>
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Randevunun bitiş saati (başlangıç + süre). Veritabanına yazılmaz.
    /// DİKKAT: LINQ sorgularının Where kısmında kullanılamaz (SQL'e çevrilemez);
    /// sorgularda AppointmentDate.AddMinutes(Duration) yazılmalıdır.
    /// </summary>
    public DateTime EndDate => AppointmentDate.AddMinutes(Duration);

    // ---------- Navigation özellikleri ----------

    public Pet Pet { get; set; } = null!;

    public ApplicationUser Owner { get; set; } = null!;

    public Veterinarian Veterinarian { get; set; } = null!;

    public Service Service { get; set; } = null!;

    /// <summary>Randevu tamamlandığında hekimin girdiği muayene notu (bire-bir, opsiyonel).</summary>
    public ExaminationNote? ExaminationNote { get; set; }
}
