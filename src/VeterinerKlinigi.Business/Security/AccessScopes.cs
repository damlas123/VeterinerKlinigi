using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Entities;

namespace VeterinerKlinigi.Business.Security;

/// <summary>
/// SAHİPLİK (kaynak bazlı yetkilendirme) KURALLARI — TEK YER.
/// Her sorgu, giriş yapmış kullanıcıya göre burada filtrelenir:
///   Admin     → tüm kayıtlar
///   Veteriner → sadece kendisiyle randevusu olan hayvanlar / kendi randevuları
///   Üye       → sadece kendi hayvanları / kendi randevuları
/// Kullanıcı adres çubuğundaki id'yi değiştirse bile bu filtre yüzünden başkasının
/// kaydı sorgu sonucuna hiç gelmez; servis "bulunamadı" (404) döndürür.
/// MVC, REST API ve MCP aynı servisleri kullandığı için bu kurallar üç kanalda da geçerlidir.
/// </summary>
public static class AccessScopes
{
    /// <summary>Kullanıcının GÖREBİLECEĞİ evcil hayvanlar.</summary>
    public static IQueryable<Pet> VisibleTo(this IQueryable<Pet> pets, ICurrentUserService user)
    {
        if (user.IsAdmin)
        {
            return pets;
        }

        if (user.IsVeterinarian && user.VeterinarianId is int veterinarianId)
        {
            return pets.Where(p => p.Appointments.Any(a => a.VeterinarianId == veterinarianId));
        }

        if (user.UserId is string userId)
        {
            return pets.Where(p => p.OwnerId == userId);
        }

        return pets.Where(_ => false);
    }

    /// <summary>Kullanıcının DÜZENLEYEBİLECEĞİ / SİLEBİLECEĞİ evcil hayvanlar (Admin veya sahibi).</summary>
    public static IQueryable<Pet> ManageableBy(this IQueryable<Pet> pets, ICurrentUserService user)
    {
        if (user.IsAdmin)
        {
            return pets;
        }

        if (user.IsMember && user.UserId is string userId)
        {
            return pets.Where(p => p.OwnerId == userId);
        }

        return pets.Where(_ => false);
    }

    /// <summary>Kullanıcının görebileceği randevular.</summary>
    public static IQueryable<Appointment> VisibleTo(this IQueryable<Appointment> appointments, ICurrentUserService user)
    {
        if (user.IsAdmin)
        {
            return appointments;
        }

        if (user.IsVeterinarian && user.VeterinarianId is int veterinarianId)
        {
            return appointments.Where(a => a.VeterinarianId == veterinarianId);
        }

        if (user.UserId is string userId)
        {
            return appointments.Where(a => a.OwnerId == userId);
        }

        return appointments.Where(_ => false);
    }

    /// <summary>Kullanıcının görebileceği aşı kayıtları (görebildiği hayvanlarınkiler).</summary>
    public static IQueryable<Vaccination> VisibleTo(this IQueryable<Vaccination> vaccinations, ICurrentUserService user)
    {
        if (user.IsAdmin)
        {
            return vaccinations;
        }

        if (user.IsVeterinarian && user.VeterinarianId is int veterinarianId)
        {
            return vaccinations.Where(v => v.Pet.Appointments.Any(a => a.VeterinarianId == veterinarianId));
        }

        if (user.UserId is string userId)
        {
            return vaccinations.Where(v => v.Pet.OwnerId == userId);
        }

        return vaccinations.Where(_ => false);
    }
}
