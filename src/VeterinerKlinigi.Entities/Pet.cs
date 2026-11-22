using VeterinerKlinigi.Entities.Enums;

namespace VeterinerKlinigi.Entities;

/// <summary>
/// Evcil hayvan. Her hayvanın bir sahibi (Üye rolündeki kullanıcı) vardır.
/// </summary>
public class Pet
{
    public int Id { get; set; }

    /// <summary>Hayvanın sahibi (AspNetUsers.Id). Sahiplik kontrolleri bu alana göre yapılır.</summary>
    public string OwnerId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public PetSpecies Species { get; set; }

    public string? Breed { get; set; }

    /// <summary>Doğum tarihi (sadece tarih, saat yok).</summary>
    public DateOnly BirthDate { get; set; }

    /// <summary>Kilo (kg).</summary>
    public decimal Weight { get; set; }

    /// <summary>Yüklenen fotoğrafın sunucudaki yolu. Örn: /uploads/pets/3f2c...e1.jpg</summary>
    public string? PhotoUrl { get; set; }

    /// <summary>false ise hayvan arşivlenmiştir; aşı ve randevu geçmişi korunur.</summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }

    public ApplicationUser Owner { get; set; } = null!;

    public ICollection<Vaccination> Vaccinations { get; set; } = new List<Vaccination>();

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
