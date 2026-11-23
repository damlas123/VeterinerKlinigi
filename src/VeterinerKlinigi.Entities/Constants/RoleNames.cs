namespace VeterinerKlinigi.Entities.Constants;

/// <summary>
/// Sistemdeki rollerin adları. Veritabanında (AspNetRoles) bu adlarla saklanır.
/// Kodda "Admin" gibi metinleri elle yazmak yerine bu sabitler kullanılır;
/// böylece yazım hatası derleme zamanında yakalanır.
/// </summary>
public static class RoleNames
{
    /// <summary>Yönetici: klinik, hizmet, veteriner ve kullanıcı yönetimi.</summary>
    public const string Admin = "Admin";

    /// <summary>Veteriner hekim: kendi randevuları, muayene notları, aşı kayıtları, müsaitlik.</summary>
    public const string Veterinarian = "Veteriner";

    /// <summary>Üye (hayvan sahibi): evcil hayvanlar, randevular, aşı takvimi.</summary>
    public const string Member = "Uye";

    /// <summary>[Authorize(Roles = ...)] içinde "Admin veya Veteriner" demek için.</summary>
    public const string AdminOrVeterinarian = Admin + "," + Veterinarian;

    /// <summary>[Authorize(Roles = ...)] içinde "Admin veya Üye" demek için.</summary>
    public const string AdminOrMember = Admin + "," + Member;

    /// <summary>Tüm roller (seed sırasında oluşturulur).</summary>
    public static readonly string[] All = [Admin, Veterinarian, Member];
}
