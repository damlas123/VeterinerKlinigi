using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VeterinerKlinigi.Data.Context;
using VeterinerKlinigi.Data.Repositories;
using VeterinerKlinigi.Data.Seed;

namespace VeterinerKlinigi.Data;

/// <summary>
/// Veri katmanının Dependency Injection kayıtları.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// AppDbContext'i SQL Server sağlayıcısıyla kaydeder.
    /// AddDbContext, DbContext'i "Scoped" olarak kaydeder: her HTTP isteği için yeni bir
    /// AppDbContext oluşturulur ve istek bitince veritabanı bağlantısı kapatılır.
    /// </summary>
    /// <param name="services">DI konteyneri.</param>
    /// <param name="connectionString">SQL Server bağlantı dizesi (Program.cs, appsettings.json'dan okur).</param>
    public static IServiceCollection AddDataLayer(this IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Veritabanı bağlantı dizesi boş olamaz.", nameof(connectionString));
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Generic repository ve Unit of Work: istek başına bir tane (DbContext ile aynı ömür).
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Başlangıç verisi (roller, admin, örnek veriler).
        services.AddScoped<DbSeeder>();

        return services;
    }
}
