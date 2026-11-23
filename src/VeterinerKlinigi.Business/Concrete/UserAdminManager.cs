using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Data.Repositories;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Business.Concrete;

public class UserAdminManager : ManagerBase, IUserAdminService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAdminManager(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        TimeProvider timeProvider,
        UserManager<ApplicationUser> userManager)
        : base(unitOfWork, currentUser, timeProvider)
    {
        _userManager = userManager;
    }

    public async Task<List<UserListItemDto>> GetUsersAsync(string? role = null, string? search = null, CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsAdmin)
        {
            return [];
        }

        var users = Repo<ApplicationUser>().QueryNoTracking();
        var userRoles = Repo<IdentityUserRole<string>>().QueryNoTracking();
        var roles = Repo<IdentityRole>().QueryNoTracking();

        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleIds = roles.Where(r => r.Name == role).Select(r => r.Id);
            users = users.Where(u => userRoles.Any(ur => ur.UserId == u.Id && roleIds.Contains(ur.RoleId)));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var text = search.Trim();
            users = users.Where(u => (u.FirstName + " " + u.LastName).Contains(text) || (u.Email != null && u.Email.Contains(text)));
        }

        var rows = await users
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                u.PhoneNumber,
                u.LockoutEnd,
                u.CreatedAt,
                Roles = roles.Where(r => userRoles.Any(ur => ur.UserId == u.Id && ur.RoleId == r.Id)).Select(r => r.Name!).ToList(),
                PetCount = u.Pets.Count(p => p.IsActive),
                AppointmentCount = u.Appointments.Count()
            })
            .ToListAsync(cancellationToken);

        var now = UtcNow;
        return rows.Select(u => new UserListItemDto(
                u.Id,
                $"{u.FirstName} {u.LastName}".Trim(),
                u.Email ?? string.Empty,
                u.PhoneNumber,
                u.Roles,
                u.LockoutEnd is not null && u.LockoutEnd > now,
                u.PetCount,
                u.AppointmentCount,
                u.CreatedAt))
            .ToList();
    }

    public async Task<ServiceResult<UserDetailDto>> GetDetailAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsAdmin)
        {
            return ServiceResult<UserDetailDto>.Forbidden();
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return ServiceResult<UserDetailDto>.NotFound("Kullanıcı bulunamadı.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var pets = await Repo<Pet>().QueryNoTracking()
            .Where(p => p.OwnerId == id)
            .OrderByDescending(p => p.IsActive).ThenBy(p => p.Name)
            .Select(Projections.Pet)
            .ToListAsync(cancellationToken);

        var appointments = await Repo<Appointment>().QueryNoTracking()
            .Where(a => a.OwnerId == id)
            .OrderByDescending(a => a.AppointmentDate)
            .Take(20)
            .Select(Projections.Appointment)
            .ToListAsync(cancellationToken);

        var summary = new UserListItemDto(
            user.Id,
            user.FullName,
            user.Email ?? string.Empty,
            user.PhoneNumber,
            roles.ToList(),
            user.LockoutEnd is not null && user.LockoutEnd > UtcNow,
            pets.Count(p => p.IsActive),
            appointments.Count,
            user.CreatedAt);

        return ServiceResult<UserDetailDto>.Success(new UserDetailDto(summary, pets, appointments));
    }

    public async Task<ServiceResult> SetLockedAsync(string id, bool locked, CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsAdmin)
        {
            return ServiceResult.Forbidden();
        }

        if (id == CurrentUser.UserId)
        {
            return ServiceResult.Invalid("Kendi hesabınızı kilitleyemezsiniz.");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user is null)
        {
            return ServiceResult.NotFound("Kullanıcı bulunamadı.");
        }

        if (await _userManager.IsInRoleAsync(user, RoleNames.Admin))
        {
            return ServiceResult.Invalid("Admin hesapları kilitlenemez.");
        }

        if (locked)
        {
            await _userManager.SetLockoutEnabledAsync(user, true);
            await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            // Güvenlik damgasını yenile: kullanıcının açık oturumları da geçersiz olur.
            await _userManager.UpdateSecurityStampAsync(user);
            return ServiceResult.Success($"{user.FullName} hesabı kilitlendi; artık giriş yapamaz.");
        }

        await _userManager.SetLockoutEndDateAsync(user, null);
        await _userManager.ResetAccessFailedCountAsync(user);
        return ServiceResult.Success($"{user.FullName} hesabının kilidi açıldı.");
    }
}
