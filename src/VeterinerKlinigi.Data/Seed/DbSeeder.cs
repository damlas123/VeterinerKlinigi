using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VeterinerKlinigi.Data.Context;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Constants;
using VeterinerKlinigi.Entities.Enums;

namespace VeterinerKlinigi.Data.Seed;

/// <summary>
/// Başlangıç verisini (seed) oluşturur: roller, admin hesabı ve örnek veriler.
/// Her adım "yoksa ekle" mantığıyla çalışır; uygulama her açıldığında tekrar çalışsa da
/// aynı kayıtları ikinci kez eklemez.
/// </summary>
public class DbSeeder
{
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<DbSeeder> _logger;

    public DbSeeder(
        AppDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        TimeProvider timeProvider,
        ILogger<DbSeeder> logger)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    private DateTime Now => _timeProvider.GetLocalNow().DateTime;

    public async Task SeedAsync(SeedOptions options)
    {
        await SeedRolesAsync();
        await SeedAdminAsync(options);

        // Örnek veriler sadece veritabanı boşken (hiç klinik yokken) eklenir.
        if (options.CreateDemoData && !await _context.Clinics.AnyAsync())
        {
            await SeedDemoDataAsync(options.DemoPassword);
            _logger.LogInformation("Örnek veriler (klinik, hizmet, veteriner, üye, randevu) eklendi.");
        }
    }

    // ------------------------------------------------------------------
    // 1) ROLLER
    // ------------------------------------------------------------------
    private async Task SeedRolesAsync()
    {
        foreach (var roleName in RoleNames.All)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }

    // ------------------------------------------------------------------
    // 2) ADMIN HESABI  (ödev şartı: ogrencinumarasi@sakarya.edu.tr / sau)
    // ------------------------------------------------------------------
    private async Task SeedAdminAsync(SeedOptions options)
    {
        var admin = await _userManager.FindByEmailAsync(options.AdminEmail);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = options.AdminEmail,
                Email = options.AdminEmail,
                EmailConfirmed = true,
                FirstName = "Sistem",
                LastName = "Yöneticisi",
                CreatedAt = Now
            };

            // Ödev, admin parolasının "sau" olmasını istiyor. Bu parola, kayıt olan herkese
            // uyguladığımız güçlü parola politikasına (en az 8 karakter, büyük/küçük harf, rakam)
            // uymaz. Politikayı herkes için gevşetmek yerine SADECE bu hesap için parolayı
            // Identity'nin PasswordHasher'ı ile hash'leyip kaydediyoruz. Parola yine düz metin
            // olarak değil, hash'lenmiş olarak saklanır.
            admin.PasswordHash = _userManager.PasswordHasher.HashPassword(admin, options.AdminPassword);

            var result = await _userManager.CreateAsync(admin);
            EnsureSucceeded(result, "Admin hesabı oluşturulamadı");
            _logger.LogInformation("Admin hesabı oluşturuldu: {Email}", options.AdminEmail);
        }

        if (!await _userManager.IsInRoleAsync(admin, RoleNames.Admin))
        {
            await _userManager.AddToRoleAsync(admin, RoleNames.Admin);
        }
    }

    // ------------------------------------------------------------------
    // 3) ÖRNEK VERİLER
    // Tarihler "bugüne göre" hesaplanır; böylece proje hangi gün açılırsa açılsın
    // yaklaşan aşılar, bekleyen randevular ve geçmiş raporlar anlamlı görünür.
    // ------------------------------------------------------------------
    private async Task SeedDemoDataAsync(string demoPassword)
    {
        var today = DateOnly.FromDateTime(Now);

        // ---------- Klinikler ----------
        var merkez = new Clinic
        {
            Name = "PatiVet Serdivan Veteriner Kliniği",
            Address = "Kemalpaşa Mah. Üniversite Cad. No: 12, Serdivan / Sakarya",
            Phone = "0264 000 00 01",
            Email = "serdivan@pativet.test",
            Description = "Küçük hayvan, egzotik hayvan ve cerrahi hizmetleri sunan, modern ekipmanlarla donatılmış ana kliniğimiz.",
            OpeningTime = new TimeOnly(9, 0),
            ClosingTime = new TimeOnly(19, 0)
        };
        var sube = new Clinic
        {
            Name = "PatiVet Adapazarı Şubesi",
            Address = "Çark Cad. No: 45, Adapazarı / Sakarya",
            Phone = "0264 000 00 02",
            Email = "adapazari@pativet.test",
            Description = "Şehir merkezindeki şubemizde muayene, aşı ve bakım hizmetleri verilmektedir.",
            OpeningTime = new TimeOnly(8, 30),
            ClosingTime = new TimeOnly(18, 0)
        };
        _context.Clinics.AddRange(merkez, sube);

        // ---------- Hizmetler ----------
        var genelMuayene = NewService(merkez, "Genel Muayene", "Genel sağlık kontrolü, kilo ve ateş ölçümü, fiziksel muayene.", 30, 750);
        var karmaAsi = NewService(merkez, "Karma Aşı", "Kedi ve köpekler için yıllık karma aşı uygulaması.", 20, 900);
        var kuduzAsi = NewService(merkez, "Kuduz Aşısı", "Yasal zorunlu kuduz aşısı.", 15, 600);
        var kisirlastirma = NewService(merkez, "Kısırlaştırma", "Genel anestezi altında kısırlaştırma operasyonu.", 120, 6500);
        var disTemizligi = NewService(merkez, "Diş Taşı Temizliği", "Ultrasonik diş taşı temizliği ve ağız kontrolü.", 60, 3000);
        var tiras = NewService(merkez, "Tıraş ve Bakım", "Tüy tıraşı, tırnak kesimi, kulak temizliği.", 60, 1200);
        var dermatoloji = NewService(merkez, "Dermatoloji Muayenesi", "Deri ve tüy hastalıklarının muayenesi.", 30, 1000);
        var egzotik = NewService(merkez, "Egzotik Hayvan Muayenesi", "Kuş, tavşan, kemirgen ve sürüngen muayenesi.", 45, 1100);
        var subeMuayene = NewService(sube, "Genel Muayene", "Genel sağlık kontrolü.", 30, 700);
        var subeAsi = NewService(sube, "Karma Aşı", "Kedi ve köpekler için karma aşı.", 20, 850);
        var subeTiras = NewService(sube, "Tıraş ve Bakım", "Tüy tıraşı ve tırnak kesimi.", 45, 1000);

        // ---------- Veterinerler (her biri bir kullanıcı hesabıdır) ----------
        var ayse = await NewVeterinarianAsync(merkez, "Ayşe", "Yılmaz", "ayse.yilmaz@pativet.test",
            "Küçük Hayvan Hekimliği", "Kedi ve köpeklerin koruyucu hekimliği ve aşılama konusunda 10 yıllık deneyim.",
            demoPassword, genelMuayene, karmaAsi, kuduzAsi, dermatoloji);
        var mehmet = await NewVeterinarianAsync(merkez, "Mehmet", "Demir", "mehmet.demir@pativet.test",
            "Cerrahi", "Yumuşak doku cerrahisi ve diş tedavileri.",
            demoPassword, genelMuayene, kisirlastirma, disTemizligi);
        var zeynep = await NewVeterinarianAsync(merkez, "Zeynep", "Kaya", "zeynep.kaya@pativet.test",
            "Dermatoloji ve Egzotik Hayvanlar", "Deri hastalıkları ile kuş, tavşan ve sürüngen hekimliği.",
            demoPassword, dermatoloji, egzotik, genelMuayene, tiras);
        var can = await NewVeterinarianAsync(sube, "Can", "Öztürk", "can.ozturk@pativet.test",
            "Küçük Hayvan Hekimliği", "Şube hekimi; muayene, aşı ve bakım hizmetleri.",
            demoPassword, subeMuayene, subeAsi, subeTiras);

        // ---------- Müsaitlik saatleri ----------
        var weekdays = new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday };
        foreach (var day in weekdays)
        {
            AddAvailability(ayse, day, 9, 0, 12, 30);
            AddAvailability(ayse, day, 13, 30, 18, 0);
        }
        AddAvailability(mehmet, DayOfWeek.Tuesday, 9, 0, 17, 0);
        AddAvailability(mehmet, DayOfWeek.Thursday, 9, 0, 17, 0);
        AddAvailability(mehmet, DayOfWeek.Saturday, 10, 0, 14, 0);
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday })
        {
            AddAvailability(zeynep, day, 10, 0, 18, 0);
        }
        foreach (var day in weekdays.Append(DayOfWeek.Saturday))
        {
            AddAvailability(can, day, 8, 30, 12, 0);
            AddAvailability(can, day, 13, 0, 17, 30);
        }

        // ---------- Üyeler ve evcil hayvanlar ----------
        var elif = await NewUserAsync("Elif", "Şahin", "uye@pativet.test", "0555 000 00 01", demoPassword, RoleNames.Member);
        var burak = await NewUserAsync("Burak", "Arslan", "uye2@pativet.test", "0555 000 00 02", demoPassword, RoleNames.Member);

        var pamuk = NewPet(elif, "Pamuk", PetSpecies.Cat, "Van Kedisi", today.AddYears(-3).AddMonths(-2), 4.2m);
        var karabas = NewPet(elif, "Karabaş", PetSpecies.Dog, "Kangal", today.AddYears(-5).AddMonths(-6), 38.5m);
        var mavis = NewPet(burak, "Maviş", PetSpecies.Bird, "Muhabbet Kuşu", today.AddYears(-1).AddMonths(-4), 0.04m);
        var tarcin = NewPet(burak, "Tarçın", PetSpecies.Rabbit, "Hollanda Lop", today.AddYears(-2), 1.8m);

        // ---------- Aşı kayıtları (yaklaşan ve gecikmiş aşılar panelde görünsün) ----------
        AddVaccination(pamuk, ayse, "Karma Aşı (FVRCP)", today.AddDays(-358), today.AddDays(7), "Yıllık tekrar dozu.");
        AddVaccination(pamuk, ayse, "Kuduz Aşısı", today.AddDays(-300), today.AddDays(65), null);
        AddVaccination(pamuk, null, "İç-Dış Parazit Uygulaması", today.AddDays(-80), today.AddDays(10), "Başka bir klinikte yapıldı.");
        AddVaccination(karabas, ayse, "Karma Aşı (DHPPi+L)", today.AddDays(-380), today.AddDays(-15), "Tekrar dozu gecikti.");
        AddVaccination(karabas, ayse, "Kuduz Aşısı", today.AddDays(-200), today.AddDays(165), null);
        AddVaccination(tarcin, zeynep, "RHD Aşısı", today.AddDays(-150), today.AddDays(215), null);

        await _context.SaveChangesAsync();

        // ---------- Randevular ----------
        // Geçmiş, tamamlanmış randevular (raporlar ve muayene geçmişi için).
        var completed = new (int DaysAgo, int Hour, Veterinarian Vet, Service Service, Pet Pet, string Diagnosis, string Treatment)[]
        {
            (150, 10, ayse, genelMuayene, pamuk, "Genel durum iyi.", "Diyet önerisi verildi."),
            (140, 11, mehmet, disTemizligi, karabas, "Diş taşı birikimi.", "Ultrasonik temizlik yapıldı."),
            (120, 14, zeynep, egzotik, mavis, "Gaga uzaması.", "Gaga törpülendi, kalsiyum takviyesi önerildi."),
            (95, 10, ayse, karmaAsi, karabas, "Aşı öncesi muayene normal.", "Karma aşı uygulandı."),
            (80, 15, zeynep, dermatoloji, pamuk, "Hafif mantar enfeksiyonu.", "Antifungal şampuan, 2 hafta."),
            (64, 9, mehmet, kisirlastirma, tarcin, "Operasyon sorunsuz.", "Ağrı kesici 5 gün, dikiş kontrolü 10 gün sonra."),
            (45, 13, zeynep, tiras, karabas, "Tüy keçeleşmesi.", "Tıraş ve bakım yapıldı."),
            (30, 10, ayse, kuduzAsi, pamuk, "Muayene normal.", "Kuduz aşısı uygulandı."),
            (21, 11, can, subeMuayene, mavis, "Rutin kontrol, sağlıklı.", "Öneri yok."),
            (12, 16, ayse, genelMuayene, karabas, "Hafif topallık.", "Antiinflamatuar, 1 hafta istirahat."),
            (6, 10, zeynep, egzotik, tarcin, "Diş kontrolü normal.", "Lifli beslenme önerildi.")
        };

        foreach (var item in completed)
        {
            var start = today.AddDays(-item.DaysAgo).ToDateTime(new TimeOnly(item.Hour, 0));
            var appointment = NewAppointment(item.Pet, item.Vet, item.Service, start, AppointmentStatus.Completed);
            appointment.ExaminationNote = new ExaminationNote
            {
                Veterinarian = item.Vet,
                Diagnosis = item.Diagnosis,
                Treatment = item.Treatment,
                CreatedAt = start.AddMinutes(item.Service.Duration)
            };
        }

        // Geçmişte iptal edilmiş bir randevu.
        var cancelled = NewAppointment(pamuk, zeynep, dermatoloji, today.AddDays(-40).ToDateTime(new TimeOnly(11, 0)), AppointmentStatus.Cancelled);
        cancelled.OwnerNote = "Kaşıntı şikâyeti.";

        // Dün (veya son iş günü) onaylanmış ama henüz tamamlanmamış randevu:
        // Veteriner panelinde "Muayeneyi tamamla" düğmesini denemek için.
        var lastWeekday = LastWeekdayBefore(today);
        NewAppointment(pamuk, ayse, karmaAsi, lastWeekday.ToDateTime(new TimeOnly(15, 0)), AppointmentStatus.Approved);

        // Gelecek randevular (müsaitlik saatlerine uygun günlerde).
        var approved = NewAppointment(pamuk, ayse, genelMuayene, NextDay(today, weekdays).ToDateTime(new TimeOnly(10, 0)), AppointmentStatus.Approved);
        approved.OwnerNote = "Yıllık kontrol.";

        var pending1 = NewAppointment(karabas, mehmet, disTemizligi,
            NextDay(today, DayOfWeek.Tuesday, DayOfWeek.Thursday).ToDateTime(new TimeOnly(14, 0)), AppointmentStatus.Pending);
        pending1.OwnerNote = "Ağız kokusu var.";

        NewAppointment(mavis, zeynep, egzotik,
            NextDay(today, DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday).ToDateTime(new TimeOnly(11, 0)), AppointmentStatus.Pending);

        var rejected = NewAppointment(karabas, ayse, kuduzAsi,
            NextDay(today, weekdays).AddDays(7).ToDateTime(new TimeOnly(9, 30)), AppointmentStatus.Rejected);
        rejected.RejectionReason = "Kuduz aşısının tekrar zamanı henüz gelmedi.";

        await _context.SaveChangesAsync();
    }

    // ------------------------------------------------------------------
    // Yardımcı metotlar
    // ------------------------------------------------------------------
    private Service NewService(Clinic clinic, string name, string description, int duration, decimal price)
    {
        var service = new Service { Clinic = clinic, Name = name, Description = description, Duration = duration, Price = price };
        _context.Services.Add(service);
        return service;
    }

    private async Task<ApplicationUser> NewUserAsync(string firstName, string lastName, string email, string? phone, string password, string role)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phone,
            CreatedAt = Now
        };

        var result = await _userManager.CreateAsync(user, password);
        EnsureSucceeded(result, $"{email} oluşturulamadı");
        await _userManager.AddToRoleAsync(user, role);
        return user;
    }

    private async Task<Veterinarian> NewVeterinarianAsync(Clinic clinic, string firstName, string lastName, string email,
        string specialty, string biography, string password, params Service[] services)
    {
        var user = await NewUserAsync(firstName, lastName, email, null, password, RoleNames.Veterinarian);
        var veterinarian = new Veterinarian
        {
            UserId = user.Id,
            Clinic = clinic,
            FullName = $"Vet. Hek. {firstName} {lastName}",
            Specialty = specialty,
            Biography = biography
        };

        foreach (var service in services)
        {
            veterinarian.VeterinarianServices.Add(new VeterinarianService { Service = service });
        }

        _context.Veterinarians.Add(veterinarian);
        return veterinarian;
    }

    private void AddAvailability(Veterinarian veterinarian, DayOfWeek day, int startHour, int startMinute, int endHour, int endMinute)
    {
        veterinarian.Availabilities.Add(new VeterinarianAvailability
        {
            DayOfWeek = day,
            StartTime = new TimeOnly(startHour, startMinute),
            EndTime = new TimeOnly(endHour, endMinute)
        });
    }

    private Pet NewPet(ApplicationUser owner, string name, PetSpecies species, string breed, DateOnly birthDate, decimal weight)
    {
        var pet = new Pet
        {
            OwnerId = owner.Id,
            Name = name,
            Species = species,
            Breed = breed,
            BirthDate = birthDate,
            Weight = weight,
            CreatedAt = Now
        };
        _context.Pets.Add(pet);
        return pet;
    }

    private void AddVaccination(Pet pet, Veterinarian? veterinarian, string name, DateOnly date, DateOnly? nextDose, string? description)
    {
        pet.Vaccinations.Add(new Vaccination
        {
            Veterinarian = veterinarian,
            VaccineName = name,
            VaccinationDate = date,
            NextDoseDate = nextDose,
            Description = description,
            CreatedAt = Now
        });
    }

    private Appointment NewAppointment(Pet pet, Veterinarian veterinarian, Service service, DateTime start, AppointmentStatus status)
    {
        var appointment = new Appointment
        {
            Pet = pet,
            OwnerId = pet.OwnerId,
            Veterinarian = veterinarian,
            Service = service,
            AppointmentDate = start,
            Duration = service.Duration,
            Price = service.Price,
            Status = status,
            CreatedAt = start < Now ? start.AddDays(-3) : Now
        };
        _context.Appointments.Add(appointment);
        return appointment;
    }

    /// <summary>Yarından itibaren, verilen günlerden birine denk gelen ilk tarih (en az 2 gün sonrası).</summary>
    private static DateOnly NextDay(DateOnly today, params DayOfWeek[] days)
    {
        var date = today.AddDays(2);
        while (!days.Contains(date.DayOfWeek))
        {
            date = date.AddDays(1);
        }
        return date;
    }

    /// <summary>Bugünden önceki son hafta içi günü.</summary>
    private static DateOnly LastWeekdayBefore(DateOnly today)
    {
        var date = today.AddDays(-1);
        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            date = date.AddDays(-1);
        }
        return date;
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"{message}: {errors}");
        }
    }
}
