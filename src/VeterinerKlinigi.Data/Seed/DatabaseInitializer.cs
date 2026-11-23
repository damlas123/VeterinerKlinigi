using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VeterinerKlinigi.Data.Context;

namespace VeterinerKlinigi.Data.Seed;

/// <summary>
/// Uygulama açılırken veritabanını hazırlar:
/// 1) Uygulanmamış migration'ları uygular (veritabanı yoksa oluşturur),
/// 2) Rolleri, admin hesabını ve (istenirse) örnek verileri ekler.
/// Böylece projeyi ilk kez açan kişi (ör. ders hocası) sadece F5'e basarak çalıştırabilir.
/// </summary>
public static class DatabaseInitializer
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, SeedOptions options)
    {
        // DbContext "scoped" bir servistir; uygulama başlangıcında bir HTTP isteği olmadığı
        // için kendi kapsamımızı (scope) oluşturup servisleri oradan alırız.
        await using var scope = services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync();

        var seeder = scope.ServiceProvider.GetRequiredService<DbSeeder>();
        await seeder.SeedAsync(options);
    }
}
