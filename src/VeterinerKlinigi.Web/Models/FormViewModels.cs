using System.ComponentModel.DataAnnotations;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Web.Models;

/// <summary>
/// Evcil hayvan formu: iş katmanının PetSaveDto'su + fotoğraf alanları.
/// Kalıtım sayesinde doğrulama kuralları (Data Annotations) tekrar yazılmaz.
/// </summary>
public class PetFormViewModel : PetSaveDto
{
    public int? Id { get; set; }

    [Display(Name = "Fotoğraf (isteğe bağlı)")]
    public IFormFile? Photo { get; set; }

    [Display(Name = "Mevcut fotoğrafı kaldır")]
    public bool RemovePhoto { get; set; }

    public string? CurrentPhotoUrl { get; set; }
}

/// <summary>
/// Randevu formu. Kullanıcı tarih ve saati ayrı ayrı seçer; controller bunları birleştirip
/// iş katmanının AppointmentSaveDto'suna çevirir.
/// </summary>
public class AppointmentFormViewModel
{
    public int? Id { get; set; }

    [Display(Name = "Evcil hayvan")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir evcil hayvan seçin.")]
    public int PetId { get; set; }

    [Display(Name = "Hizmet")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir hizmet seçin.")]
    public int ServiceId { get; set; }

    /// <summary>Saat seçilirken JavaScript tarafından doldurulur.</summary>
    [Display(Name = "Veteriner")]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen listeden bir saat seçin.")]
    public int VeterinarianId { get; set; }

    [Display(Name = "Tarih")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    public DateOnly Date { get; set; }

    /// <summary>"10:30" biçiminde; JavaScript ile seçilen saat düğmesinden gelir.</summary>
    [Display(Name = "Saat")]
    [Required(ErrorMessage = "Lütfen listeden bir saat seçin.")]
    [RegularExpression(@"^([01]\d|2[0-3]):[0-5]\d$", ErrorMessage = "Geçerli bir saat seçin.")]
    public string? Time { get; set; }

    [Display(Name = "Notunuz / şikâyet")]
    [StringLength(FieldLengths.Note, ErrorMessage = ValidationMessages.MaxLength)]
    public string? OwnerNote { get; set; }
}

/// <summary>Silme onay sayfası (tüm modüller için ortak görünüm: Views/Shared/DeleteConfirm.cshtml).</summary>
public class DeleteConfirmViewModel
{
    public string Id { get; set; } = string.Empty;

    public string Title { get; set; } = "Silme onayı";

    public string Question { get; set; } = "Bu kaydı silmek istediğinize emin misiniz?";

    public List<KeyValuePair<string, string>> Details { get; set; } = [];

    /// <summary>Kaydın bağlı geçmişi varsa "silinmeyecek, arşivlenecek" uyarısı.</summary>
    public string? Note { get; set; }

    public string CancelUrl { get; set; } = "/";
}

/// <summary>Ana sayfa verileri.</summary>
public record HomeViewModel(List<ServiceDto> Services, List<VeterinarianDto> Veterinarians, List<ClinicDto> Clinics);

/// <summary>Veterinerlerimiz sayfası: filtreler + liste.</summary>
public record VeterinariansPageViewModel(
    List<VeterinarianDto> Veterinarians,
    List<ClinicDto> Clinics,
    List<ServiceDto> Services,
    List<string> Specialties,
    int? ClinicId,
    int? ServiceId,
    string? Specialty);

/// <summary>Hizmetlerimiz sayfası.</summary>
public record ServicesPageViewModel(List<ClinicDto> Clinics, List<ServiceDto> Services, int? ClinicId);
