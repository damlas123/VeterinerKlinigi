using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Entities.Enums;

/// <summary>
/// Randevunun yaşam döngüsü. Veritabanında metin olarak saklanır ("Pending", "Approved" ...).
/// [Display] içindeki Türkçe adlar arayüzde gösterilir.
/// </summary>
public enum AppointmentStatus
{
    /// <summary>Randevu oluşturuldu, Admin veya ilgili veterinerin onayını bekliyor.</summary>
    [Display(Name = "Bekliyor")]
    Pending = 0,

    /// <summary>Admin veya ilgili veteriner onayladı.</summary>
    [Display(Name = "Onaylandı")]
    Approved = 1,

    /// <summary>Admin veya ilgili veteriner reddetti (gerekçe zorunlu). Saat tekrar boşa çıkar.</summary>
    [Display(Name = "Reddedildi")]
    Rejected = 2,

    /// <summary>Muayene yapıldı, veteriner muayene notunu girdi.</summary>
    [Display(Name = "Tamamlandı")]
    Completed = 3,

    /// <summary>Hayvan sahibi veya Admin iptal etti. Saat tekrar boşa çıkar.</summary>
    [Display(Name = "İptal Edildi")]
    Cancelled = 4
}
