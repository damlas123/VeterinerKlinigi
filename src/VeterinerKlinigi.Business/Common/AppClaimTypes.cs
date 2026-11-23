namespace VeterinerKlinigi.Business.Common;

/// <summary>
/// Oturum çerezine ve JWT token'a eklediğimiz özel claim (kimlik bilgisi) adları.
/// </summary>
public static class AppClaimTypes
{
    /// <summary>Kullanıcı bir veteriner ise Veterinarians tablosundaki Id'si.</summary>
    public const string VeterinarianId = "vet_id";

    /// <summary>Kullanıcının adı ve soyadı (menüde göstermek için).</summary>
    public const string FullName = "full_name";
}
