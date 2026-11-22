using Microsoft.AspNetCore.Identity;

namespace VeterinerKlinigi.Entities;

/// <summary>
/// Sistemdeki her kullanıcı (Admin, Veteriner, Üye). Identity'nin AspNetUsers tablosuna karşılık gelir.
/// IdentityUser'dan miras aldığı için Id, Email, UserName, PasswordHash, PhoneNumber,
/// LockoutEnd gibi alanlar hazır gelir; biz sadece ek alanları tanımlarız.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    /// <summary>Hesabın oluşturulma zamanı.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Ad ve soyadın birleşimi. Hesaplanan bir özelliktir, veritabanına yazılmaz.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    // ---------- Navigation (ilişki) özellikleri ----------

    /// <summary>Kullanıcı bir veteriner ise hekim kaydı (bire-bir, opsiyonel).</summary>
    public Veterinarian? Veterinarian { get; set; }

    /// <summary>Kullanıcının sahibi olduğu evcil hayvanlar (bire-çok).</summary>
    public ICollection<Pet> Pets { get; set; } = new List<Pet>();

    /// <summary>Kullanıcının hayvan sahibi olarak oluşturduğu randevular (bire-çok).</summary>
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
