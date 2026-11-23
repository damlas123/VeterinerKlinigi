using System.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace VeterinerKlinigi.Data.Repositories;

/// <summary>
/// Unit of Work (iş birimi): Bir iş adımı içinde yapılan tüm değişiklikleri
/// TEK seferde kaydeder ve gerektiğinde transaction (işlem bütünlüğü) başlatır.
/// Tüm repository'ler aynı DbContext'i paylaşır; böylece "randevu ekle + hizmet fiyatını oku"
/// gibi adımlar aynı bağlantı ve aynı transaction içinde çalışır.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>İstenen entity tipi için repository döndürür.</summary>
    IRepository<TEntity> Repository<TEntity>() where TEntity : class;

    /// <summary>Bekleyen tüm değişiklikleri veritabanına yazar.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Veritabanı transaction'ı başlatır. Commit edilmezse (hata olursa) tüm değişiklikler geri alınır.
    /// Randevu çakışma kontrolünde IsolationLevel.Serializable kullanılır.
    /// </summary>
    Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default);
}
