namespace VeterinerKlinigi.Data.Repositories;

/// <summary>
/// Tüm entity'ler için ortak (generic) veri erişim arayüzü.
/// Business katmanı veritabanına doğrudan değil, bu arayüz üzerinden erişir.
/// </summary>
/// <typeparam name="TEntity">Entity tipi (Pet, Appointment ...).</typeparam>
public interface IRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Değişiklik takibi (tracking) AÇIK sorgu. Okunan kayıtlar güncellenecekse kullanılır;
    /// EF Core nesnedeki değişiklikleri SaveChanges sırasında otomatik algılar.
    /// </summary>
    IQueryable<TEntity> Query();

    /// <summary>
    /// Değişiklik takibi KAPALI sorgu. Sadece okuma (listeleme, rapor) için kullanılır; daha hızlıdır.
    /// </summary>
    IQueryable<TEntity> QueryNoTracking();

    /// <summary>Birincil anahtara göre kayıt getirir (bulunamazsa null).</summary>
    ValueTask<TEntity?> FindAsync(object[] keyValues, CancellationToken cancellationToken = default);

    /// <summary>Yeni kaydı ekleme için işaretler (veritabanına SaveChanges ile yazılır).</summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Kaydı güncelleme için işaretler.</summary>
    void Update(TEntity entity);

    /// <summary>Kaydı silme için işaretler.</summary>
    void Remove(TEntity entity);

    /// <summary>Birden fazla kaydı silme için işaretler.</summary>
    void RemoveRange(IEnumerable<TEntity> entities);
}
