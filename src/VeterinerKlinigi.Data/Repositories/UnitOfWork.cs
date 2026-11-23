using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using VeterinerKlinigi.Data.Context;

namespace VeterinerKlinigi.Data.Repositories;

/// <summary>
/// IUnitOfWork'ün EF Core uygulaması. DbContext zaten bir "unit of work" olduğu için
/// bu sınıf onun üzerinde ince bir katmandır.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    // Aynı istek içinde aynı tip için tekrar tekrar repository oluşturmamak için önbellek.
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRepository<TEntity> Repository<TEntity>() where TEntity : class
    {
        if (!_repositories.TryGetValue(typeof(TEntity), out var repository))
        {
            repository = new Repository<TEntity>(_context);
            _repositories[typeof(TEntity)] = repository;
        }

        return (IRepository<TEntity>)repository;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);

    public Task<IDbContextTransaction> BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default)
        => _context.Database.BeginTransactionAsync(isolationLevel, cancellationToken);
}
