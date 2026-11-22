using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Entities;

namespace VeterinerKlinigi.Data.Context;

/// <summary>
/// Uygulamanın veritabanı bağlamı (Code First).
/// IdentityDbContext'ten miras aldığı için Identity tablolarını (AspNetUsers, AspNetRoles ...)
/// otomatik olarak içerir; biz sadece kendi tablolarımızı ekleriz.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    // DbContextOptions (bağlantı dizesi, SQL Server sağlayıcısı) Program.cs'te
    // AddDataLayer() ile ayarlanır ve Dependency Injection ile buraya gelir.
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Her DbSet bir tabloyu temsil eder. LINQ sorguları bu özellikler üzerinden yazılır:
    // örn. context.Pets.Where(p => p.OwnerId == userId)
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Veterinarian> Veterinarians => Set<Veterinarian>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<VeterinarianService> VeterinarianServices => Set<VeterinarianService>();
    public DbSet<VeterinarianAvailability> VeterinarianAvailabilities => Set<VeterinarianAvailability>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<Vaccination> Vaccinations => Set<Vaccination>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ExaminationNote> ExaminationNotes => Set<ExaminationNote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // 1) Önce Identity tablolarının ayarları (bunu çağırmazsak Identity tabloları hatalı oluşur).
        base.OnModelCreating(builder);

        // 2) Configurations klasöründeki tüm IEntityTypeConfiguration<T> sınıflarını uygular.
        //    Her entity'nin ayarı kendi dosyasında durur; bu dosya kalabalıklaşmaz.
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Tüm DateTime kolonları datetime2(0): saniyeden küçük kısım (milisaniye) saklanmaz.
        configurationBuilder.Properties<DateTime>().HavePrecision(0);

        // Tüm TimeOnly kolonları time(0): 09:30:00 gibi saniye hassasiyetinde saklanır.
        configurationBuilder.Properties<TimeOnly>().HavePrecision(0);
    }
}
