namespace VeterinerKlinigi.Entities;

/// <summary>
/// Veteriner hekim. Her hekim sisteme giriş yapabilen bir kullanıcıya (ApplicationUser) bağlıdır.
/// </summary>
public class Veterinarian
{
    public int Id { get; set; }

    /// <summary>Hekimin giriş hesabı (AspNetUsers.Id). Bir kullanıcının en fazla bir hekim kaydı olabilir.</summary>
    public string UserId { get; set; } = string.Empty;

    public int ClinicId { get; set; }

    /// <summary>Unvanıyla birlikte görünen ad. Örn: "Vet. Hek. Ayşe Yılmaz".</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Uzmanlık alanı. Örn: Küçük hayvan, Egzotik hayvan, Cerrahi, Dermatoloji.</summary>
    public string Specialty { get; set; } = string.Empty;

    public string? Biography { get; set; }

    /// <summary>false ise hekim arşivlenmiştir; yeni randevu alamaz, geçmiş kayıtları korunur.</summary>
    public bool IsActive { get; set; } = true;

    // ---------- Navigation özellikleri ----------
    // "= null!" : Bu özellikler EF Core tarafından veritabanından doldurulur.
    // Derleyiciye "bunlar null olmayacak" bilgisini veririz (nullable uyarısını kapatır).

    public ApplicationUser User { get; set; } = null!;

    public Clinic Clinic { get; set; } = null!;

    /// <summary>Hekimin verebildiği hizmetler (çoka-çok ara tablo).</summary>
    public ICollection<VeterinarianService> VeterinarianServices { get; set; } = new List<VeterinarianService>();

    /// <summary>Haftalık çalışma (müsaitlik) saatleri.</summary>
    public ICollection<VeterinarianAvailability> Availabilities { get; set; } = new List<VeterinarianAvailability>();

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public ICollection<ExaminationNote> ExaminationNotes { get; set; } = new List<ExaminationNote>();

    /// <summary>Bu hekimin uyguladığı aşılar.</summary>
    public ICollection<Vaccination> Vaccinations { get; set; } = new List<Vaccination>();
}
