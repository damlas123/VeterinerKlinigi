namespace VeterinerKlinigi.Data.Seed;

/// <summary>
/// Başlangıç verisi ayarları. appsettings.json içindeki "Seed" bölümünden okunur.
/// </summary>
public class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>
    /// Admin hesabının e-postası. Ödev şartı: ogrencinumarasi@sakarya.edu.tr
    /// </summary>
    public string AdminEmail { get; set; } = "b241210351@sakarya.edu.tr";

    /// <summary>Admin parolası. Ödev şartı: "sau".</summary>
    public string AdminPassword { get; set; } = "sau";

    /// <summary>true ise veritabanı boşken örnek klinik, hizmet, veteriner, üye ve randevular eklenir.</summary>
    public bool CreateDemoData { get; set; } = true;

    /// <summary>Örnek veteriner ve üye hesaplarının ortak parolası (sadece demo verisi için).</summary>
    public string DemoPassword { get; set; } = "Demo123!";
}
