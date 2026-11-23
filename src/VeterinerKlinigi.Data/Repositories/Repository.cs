using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data.Context;

namespace VeterinerKlinigi.Data.Repositories;

/// <summary>
/// IRepository'nin EF Core ile yazılmış genel uygulaması.
/// Her entity için ayrı bir repository sınıfı yazmak yerine bu tek sınıf kullanılır.
/// </summary>
public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    private readonly DbSet<TEntity> _set;

    public Repository(AppDbContext context)
    {
        _set = context.Set<TEntity>();
    }

    public IQueryable<TEntity> Query() => _set;

    public IQueryable<TEntity> QueryNoTracking() => _set.AsNoTracking();

    public ValueTask<TEntity?> FindAsync(object[] keyValues, CancellationToken cancellationToken = default)
        => _set.FindAsync(keyValues, cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
        => await _set.AddAsync(entity, cancellationToken);

    public void Update(TEntity entity) => _set.Update(entity);

    public void Remove(TEntity entity) => _set.Remove(entity);

    public void RemoveRange(IEnumerable<TEntity> entities) => _set.RemoveRange(entities);
}
