using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.Business.Abstract;

/// <summary>Admin: kullanıcı (hayvan sahibi) yönetimi.</summary>
public interface IUserAdminService
{
    Task<List<UserListItemDto>> GetUsersAsync(string? role = null, string? search = null, CancellationToken cancellationToken = default);

    Task<ServiceResult<UserDetailDto>> GetDetailAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>Hesabı kilitler (giriş yapamaz) veya kilidi açar.</summary>
    Task<ServiceResult> SetLockedAsync(string id, bool locked, CancellationToken cancellationToken = default);
}
