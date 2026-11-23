using System.ComponentModel.DataAnnotations;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Business.Dtos;

/// <summary>Muayene notu.</summary>
public record ExaminationNoteDto(
    int Id,
    int AppointmentId,
    int VeterinarianId,
    string VeterinarianName,
    int PetId,
    string PetName,
    string ServiceName,
    DateTime AppointmentDate,
    string Diagnosis,
    string Treatment,
    string? Notes,
    DateTime CreatedAt);

/// <summary>Muayene notu formu (randevuyu tamamlarken veya notu düzenlerken).</summary>
public class ExaminationNoteSaveDto
{
    [Display(Name = "Tanı")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.Diagnosis, ErrorMessage = ValidationMessages.MaxLength)]
    public string Diagnosis { get; set; } = string.Empty;

    [Display(Name = "Tedavi")]
    [Required(ErrorMessage = ValidationMessages.Required)]
    [StringLength(FieldLengths.Treatment, ErrorMessage = ValidationMessages.MaxLength)]
    public string Treatment { get; set; } = string.Empty;

    [Display(Name = "Ek notlar")]
    [StringLength(FieldLengths.ExaminationNotes, ErrorMessage = ValidationMessages.MaxLength)]
    public string? Notes { get; set; }
}
