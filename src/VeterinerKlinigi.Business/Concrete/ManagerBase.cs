using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Data.Repositories;

namespace VeterinerKlinigi.Business.Concrete;

/// <summary>
/// Tüm iş servislerinin (Manager) ortak taban sınıfı.
/// Unit of Work, giriş yapan kullanıcı ve "şu an" bilgisine erişimi tek yerde toplar.
/// </summary>
public abstract class ManagerBase
{
    private readonly TimeProvider _timeProvider;

    protected ManagerBase(IUnitOfWork unitOfWork, ICurrentUserService currentUser, TimeProvider timeProvider)
    {
        UnitOfWork = unitOfWork;
        CurrentUser = currentUser;
        _timeProvider = timeProvider;
    }

    protected IUnitOfWork UnitOfWork { get; }

    protected ICurrentUserService CurrentUser { get; }

    /// <summary>Klinik yerel saatiyle şu an (saniyenin altı atılır).</summary>
    protected DateTime Now
    {
        get
        {
            var now = _timeProvider.GetLocalNow().DateTime;
            return new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Unspecified);
        }
    }

    /// <summary>UTC olarak şu an (Identity'nin kilit süresi gibi UTC tutulan alanlar için).</summary>
    protected DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    /// <summary>Bugünün tarihi.</summary>
    protected DateOnly Today => DateOnly.FromDateTime(Now);

    /// <summary>Kısa yol: UnitOfWork.Repository&lt;T&gt;().</summary>
    protected IRepository<TEntity> Repo<TEntity>() where TEntity : class => UnitOfWork.Repository<TEntity>();
}
