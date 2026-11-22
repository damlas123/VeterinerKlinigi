namespace VeterinerKlinigi.Entities;

/// <summary>
/// Evcil hayvanın aşı kaydı.
/// </summary>
public class Vaccination
{
    public int Id { get; set; }

    public int PetId { get; set; }

    /// <summary>Aşıyı uygulayan hekim. Aşı başka bir klinikte yapıldıysa boş (null) olabilir.</summary>
    public int? VeterinarianId { get; set; }

    public string VaccineName { get; set; } = string.Empty;

    /// <summary>Aşının yapıldığı tarih.</summary>
    public DateOnly VaccinationDate { get; set; }

    /// <summary>Bir sonraki doz tarihi (varsa). "Yaklaşan aşılar" bu alana göre listelenir.</summary>
    public DateOnly? NextDoseDate { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public Pet Pet { get; set; } = null!;

    public Veterinarian? Veterinarian { get; set; }
}
