using System.ComponentModel.DataAnnotations;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Entities.Constants;
using VeterinerKlinigi.Entities.Enums;

namespace VeterinerKlinigi.Business.Dtos;

/// <summary>Randevu bilgisi.</summary>
public record AppointmentDto(
    int Id,
    int PetId,
    string PetName,
    PetSpecies Species,
    string OwnerId,
    string OwnerName,
    int VeterinarianId,
    string VeterinarianName,
    int ServiceId,
    string ServiceName,
    string ClinicName,
    DateTime AppointmentDate,
    int Duration,
    decimal Price,
    AppointmentStatus Status,
    string? OwnerNote,
    string? RejectionReason,
    DateTime CreatedAt,
    bool HasExaminationNote)
{
    /// <summary>Bitiş saati = başlangıç + süre.</summary>
    public DateTime EndDate => AppointmentDate.AddMinutes(Duration);

    /// <summary>Durumun Türkçe adı ("Bekliyor").</summary>
    public string StatusName => DisplayText.Status(Status);
}

/// <summary>Randevu detayı ve giriş yapan kullanıcının bu randevuda yapabileceği işlemler.</summary>
public record AppointmentDetailDto(
    AppointmentDto Appointment,
    ExaminationNoteDto? ExaminationNote,
    string OwnerEmail,
    string? OwnerPhone,
    bool CanApprove,
    bool CanReject,
    bool CanCancel,
    bool CanEdit,
    bool CanComplete,
    bool CanDelete);

/// <summary>Randevu listesini filtrelemek için.</summary>
public class AppointmentFilter
{
    public AppointmentStatus? Status { get; set; }

    public DateOnly? From { get; set; }

    public DateOnly? To { get; set; }

    public int? VeterinarianId { get; set; }

    public int? PetId { get; set; }

    public int? ClinicId { get; set; }
}

/// <summary>
/// Randevu oluşturma / güncelleme isteği.
/// MVC formu, REST API (POST /api/appointments) ve MCP aracı (randevu_olustur) aynı modeli kullanır.
/// </summary>
public class AppointmentSaveDto
{
    [Display(Name = "Evcil hayvan")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir evcil hayvan seçin.")]
    public int PetId { get; set; }

    [Display(Name = "Hizmet")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir hizmet seçin.")]
    public int ServiceId { get; set; }

    [Display(Name = "Veteriner")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [Range(1, int.MaxValue, ErrorMessage = "Lütfen bir veteriner seçin.")]
    public int VeterinarianId { get; set; }

    /// <summary>Randevunun başlangıç tarihi ve saati (ör. 2026-11-25T10:30).</summary>
    [Display(Name = "Randevu tarihi ve saati")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    public DateTime AppointmentDate { get; set; }

    [Display(Name = "Notunuz / şikâyet")]
    [StringLength(FieldLengths.Note, ErrorMessage = ValidationMessages.MaxLength)]
    public string? OwnerNote { get; set; }
}

/// <summary>Randevu reddetme isteği (gerekçe zorunlu).</summary>
public class RejectAppointmentDto
{
    [Display(Name = "Ret gerekçesi")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.Note, MinimumLength = 5, ErrorMessage = ValidationMessages.LengthRange)]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>Bir hekimin seçilen gündeki boş başlangıç saatleri.</summary>
public record VeterinarianSlotsDto(
    int VeterinarianId,
    string VeterinarianName,
    string Specialty,
    IReadOnlyList<string> Slots);
