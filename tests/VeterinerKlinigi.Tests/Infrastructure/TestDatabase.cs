using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Data.Context;
using VeterinerKlinigi.Data.Repositories;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Constants;
using VeterinerKlinigi.Entities.Enums;

namespace VeterinerKlinigi.Tests.Infrastructure;

/// <summary>
/// Her test için sıfırdan oluşturulan, bellekte çalışan bir SQLite veritabanı.
/// Uygulamanın kendi AppDbContext'i ve Fluent API ayarları kullanılır; böylece ilişkiler,
/// kısıtlar ve LINQ sorguları gerçek bir ilişkisel veritabanında test edilir.
///
/// Örnek veri (sabit tarih: "şimdi" = 1 Aralık 2026 Salı 08:00):
///   Klinik 09:00-18:00 · Hizmet "Genel Muayene" 30 dk / 750 ₺ · Hizmet "Kısırlaştırma" 120 dk
///   Veteriner Ayşe: her gün 09:00-17:00, iki hizmeti de verir
///   Veteriner Mehmet: sadece Kısırlaştırma, çalışma saati yok
///   Üye 1 → Pamuk · Üye 2 → Maviş
/// </summary>
public sealed class TestDatabase : IDisposable
{
    public static readonly DateTimeOffset FixedNow = new(2026, 12, 1, 8, 0, 0, TimeSpan.Zero);

    /// <summary>Testlerde kullanılan randevu günü: "şimdi"den bir gün sonrası (2 Aralık 2026 Çarşamba).</summary>
    public static readonly DateOnly Tomorrow = new(2026, 12, 2);

    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        // Bağlantı açık kaldığı sürece in-memory veritabanı yaşar.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var context = CreateContext();
        context.Database.EnsureCreated();
        Seed(context);
    }

    public string Member1Id { get; } = "user-member-1";
    public string Member2Id { get; } = "user-member-2";
    public string VetUserId { get; } = "user-vet-1";
    public string Vet2UserId { get; } = "user-vet-2";

    public int ClinicId { get; private set; }
    public int ExamServiceId { get; private set; }
    public int SurgeryServiceId { get; private set; }
    public int VetId { get; private set; }
    public int Vet2Id { get; private set; }
    public int Member1PetId { get; private set; }
    public int Member2PetId { get; private set; }

    public AppDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);

    public IUnitOfWork CreateUnitOfWork(AppDbContext context) => new UnitOfWork(context);

    public static TimeProvider Clock { get; } = new FixedTimeProvider(FixedNow);

    public ICurrentUserService Member1 => new TestCurrentUser(Member1Id, null, RoleNames.Member);
    public ICurrentUserService Member2 => new TestCurrentUser(Member2Id, null, RoleNames.Member);
    public ICurrentUserService Vet => new TestCurrentUser(VetUserId, VetId, RoleNames.Veterinarian);
    public ICurrentUserService Vet2 => new TestCurrentUser(Vet2UserId, Vet2Id, RoleNames.Veterinarian);
    public static ICurrentUserService Admin => new TestCurrentUser("user-admin", null, RoleNames.Admin);

    private void Seed(AppDbContext context)
    {
        var created = FixedNow.UtcDateTime;
        ApplicationUser User(string id, string first) => new()
        {
            Id = id,
            UserName = id + "@test.local",
            Email = id + "@test.local",
            FirstName = first,
            LastName = "Test",
            CreatedAt = created
        };

        context.Users.AddRange(User(Member1Id, "Elif"), User(Member2Id, "Burak"), User(VetUserId, "Ayşe"), User(Vet2UserId, "Mehmet"));

        var clinic = new Clinic
        {
            Name = "Test Kliniği",
            Address = "Serdivan",
            Phone = "0264 000 00 00",
            OpeningTime = new TimeOnly(9, 0),
            ClosingTime = new TimeOnly(18, 0)
        };
        var exam = new Service { Clinic = clinic, Name = "Genel Muayene", Duration = 30, Price = 750m };
        var surgery = new Service { Clinic = clinic, Name = "Kısırlaştırma", Duration = 120, Price = 6500m };

        var vet = new Veterinarian { UserId = VetUserId, Clinic = clinic, FullName = "Vet. Hek. Ayşe Yılmaz", Specialty = "Küçük hayvan" };
        var vet2 = new Veterinarian { UserId = Vet2UserId, Clinic = clinic, FullName = "Vet. Hek. Mehmet Demir", Specialty = "Cerrahi" };
        vet.VeterinarianServices.Add(new VeterinarianService { Service = exam });
        vet.VeterinarianServices.Add(new VeterinarianService { Service = surgery });
        vet2.VeterinarianServices.Add(new VeterinarianService { Service = surgery });
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            vet.Availabilities.Add(new VeterinarianAvailability { DayOfWeek = day, StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(17, 0) });
        }

        var pet1 = new Pet { OwnerId = Member1Id, Name = "Pamuk", Species = PetSpecies.Cat, BirthDate = new DateOnly(2023, 5, 1), Weight = 4.2m, CreatedAt = created };
        var pet2 = new Pet { OwnerId = Member2Id, Name = "Maviş", Species = PetSpecies.Bird, BirthDate = new DateOnly(2025, 1, 1), Weight = 0.04m, CreatedAt = created };

        context.AddRange(clinic, exam, surgery, vet, vet2, pet1, pet2);
        context.SaveChanges();

        ClinicId = clinic.Id;
        ExamServiceId = exam.Id;
        SurgeryServiceId = surgery.Id;
        VetId = vet.Id;
        Vet2Id = vet2.Id;
        Member1PetId = pet1.Id;
        Member2PetId = pet2.Id;
    }

    public void Dispose() => _connection.Dispose();

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}

/// <summary>Giriş yapmış kullanıcıyı taklit eder (HttpContext olmadan servisleri test etmek için).</summary>
public sealed class TestCurrentUser(string? userId, int? veterinarianId, params string[] roles) : ICurrentUserService
{
    public string? UserId { get; } = userId;

    public int? VeterinarianId { get; } = veterinarianId;

    public bool IsAuthenticated => UserId is not null;

    public bool IsInRole(string role) => roles.Contains(role);
}
