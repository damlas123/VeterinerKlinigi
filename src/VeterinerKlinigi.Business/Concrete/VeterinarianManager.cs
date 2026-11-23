using System.Data;
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

public class VeterinarianManager : ManagerBase, IVeterinarianService
{
    private readonly UserManager<ApplicationUser> _userManager;

    public VeterinarianManager(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        TimeProvider timeProvider,
        UserManager<ApplicationUser> userManager)
        : base(unitOfWork, currentUser, timeProvider)
    {
        _userManager = userManager;
    }

    private IRepository<Veterinarian> Veterinarians => Repo<Veterinarian>();

    public async Task<List<VeterinarianDto>> GetAllAsync(VeterinarianFilter? filter = null, CancellationToken cancellationToken = default)
    {
        filter ??= new VeterinarianFilter();
        var showInactive = filter.IncludeInactive && CurrentUser.IsAdmin;

        // LINQ ile filtreleme: her koşul sadece ilgili filtre doluysa sorguya eklenir.
        var query = Veterinarians.QueryNoTracking()
            .Where(v => showInactive || (v.IsActive && v.Clinic.IsActive));

        if (filter.ClinicId is int clinicId)
        {
            query = query.Where(v => v.ClinicId == clinicId);
        }

        if (filter.ServiceId is int serviceId)
        {
            query = query.Where(v => v.VeterinarianServices.Any(vs => vs.ServiceId == serviceId && vs.Service.IsActive));
        }

        if (!string.IsNullOrWhiteSpace(filter.Specialty))
        {
            var specialty = filter.Specialty.Trim();
            query = query.Where(v => v.Specialty.Contains(specialty));
        }

        return await query
            .OrderBy(v => v.FullName)
            .Select(Projections.Veterinarian)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<VeterinarianDetailDto>> GetDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var isAdmin = CurrentUser.IsAdmin;
        var veterinarian = await Veterinarians.QueryNoTracking()
            .Where(v => v.Id == id && (isAdmin || v.IsActive))
            .Select(Projections.Veterinarian)
            .FirstOrDefaultAsync(cancellationToken);

        if (veterinarian is null)
        {
            return ServiceResult<VeterinarianDetailDto>.NotFound("Veteriner bulunamadı.");
        }

        var availabilities = await Repo<VeterinarianAvailability>().QueryNoTracking()
            .Where(a => a.VeterinarianId == id)
            .Select(Projections.Availability)
            .ToListAsync(cancellationToken);

        var ordered = availabilities
            .OrderBy(a => ((int)a.DayOfWeek + 6) % 7) // Pazartesi ilk sırada
            .ThenBy(a => a.StartTime)
            .ToList();

        return ServiceResult<VeterinarianDetailDto>.Success(new VeterinarianDetailDto(veterinarian, ordered));
    }

    public async Task<ServiceResult<VeterinarianUpdateDto>> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsAdmin)
        {
            return ServiceResult<VeterinarianUpdateDto>.Forbidden();
        }

        var dto = await Veterinarians.QueryNoTracking()
            .Where(v => v.Id == id)
            .Select(v => new VeterinarianUpdateDto
            {
                FullName = v.FullName,
                Specialty = v.Specialty,
                Biography = v.Biography,
                PhoneNumber = v.User.PhoneNumber,
                ClinicId = v.ClinicId,
                IsActive = v.IsActive,
                ServiceIds = v.VeterinarianServices.Select(vs => vs.ServiceId).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? ServiceResult<VeterinarianUpdateDto>.NotFound("Veteriner bulunamadı.")
            : ServiceResult<VeterinarianUpdateDto>.Success(dto);
    }

    public async Task<List<string>> GetSpecialtiesAsync(CancellationToken cancellationToken = default)
    {
        return await Veterinarians.QueryNoTracking()
            .Where(v => v.IsActive)
            .Select(v => v.Specialty)
            .Distinct()
            .OrderBy(s => s)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<int>> CreateAsync(VeterinarianCreateDto dto, CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsAdmin)
        {
            return ServiceResult<int>.Forbidden();
        }

        var clinicExists = await Repo<Clinic>().QueryNoTracking().AnyAsync(c => c.Id == dto.ClinicId, cancellationToken);
        if (!clinicExists)
        {
            return ServiceResult<int>.Invalid("Seçilen klinik bulunamadı.", nameof(dto.ClinicId));
        }

        var email = dto.Email.Trim();
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return ServiceResult<int>.Conflict("Bu e-posta adresiyle kayıtlı bir kullanıcı zaten var.", nameof(dto.Email));
        }

        var serviceIds = await FilterServicesOfClinicAsync(dto.ServiceIds, dto.ClinicId, cancellationToken);

        // Kullanıcı hesabı ve hekim kaydı birlikte oluşmalı: biri başarısız olursa ikisi de geri alınır.
        await using var transaction = await UnitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
            CreatedAt = Now
        };

        // Parola, Identity'nin parola politikasına göre doğrulanır ve hash'lenerek saklanır.
        var createResult = await _userManager.CreateAsync(user, dto.Password);
        if (!createResult.Succeeded)
        {
            return ServiceResult<int>.Invalid(string.Join(" ", createResult.Errors.Select(e => e.Description)), nameof(dto.Password));
        }

        await _userManager.AddToRoleAsync(user, RoleNames.Veterinarian);

        var veterinarian = new Veterinarian
        {
            UserId = user.Id,
            ClinicId = dto.ClinicId,
            FullName = dto.FullName.Trim(),
            Specialty = dto.Specialty.Trim(),
            Biography = string.IsNullOrWhiteSpace(dto.Biography) ? null : dto.Biography.Trim(),
            VeterinarianServices = serviceIds.Select(id => new VeterinarianService { ServiceId = id }).ToList()
        };

        await Veterinarians.AddAsync(veterinarian, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ServiceResult<int>.Success(veterinarian.Id, $"{veterinarian.FullName} eklendi. Giriş e-postası: {email}");
    }

    public async Task<ServiceResult> UpdateAsync(int id, VeterinarianUpdateDto dto, CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsAdmin)
        {
            return ServiceResult.Forbidden();
        }

        var veterinarian = await Veterinarians.Query()
            .Include(v => v.User)
            .Include(v => v.VeterinarianServices)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (veterinarian is null)
        {
            return ServiceResult.NotFound("Veteriner bulunamadı.");
        }

        var clinicExists = await Repo<Clinic>().QueryNoTracking().AnyAsync(c => c.Id == dto.ClinicId, cancellationToken);
        if (!clinicExists)
        {
            return ServiceResult.Invalid("Seçilen klinik bulunamadı.", nameof(dto.ClinicId));
        }

        var serviceIds = await FilterServicesOfClinicAsync(dto.ServiceIds, dto.ClinicId, cancellationToken);

        veterinarian.FullName = dto.FullName.Trim();
        veterinarian.Specialty = dto.Specialty.Trim();
        veterinarian.Biography = string.IsNullOrWhiteSpace(dto.Biography) ? null : dto.Biography.Trim();
        veterinarian.ClinicId = dto.ClinicId;
        veterinarian.IsActive = dto.IsActive;
        veterinarian.User.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();

        // Hizmet eşleşmelerini senkronize et: listede olmayanları sil, yenileri ekle.
        var toRemove = veterinarian.VeterinarianServices.Where(vs => !serviceIds.Contains(vs.ServiceId)).ToList();
        Repo<VeterinarianService>().RemoveRange(toRemove);

        var existing = veterinarian.VeterinarianServices.Select(vs => vs.ServiceId).ToHashSet();
        foreach (var serviceId in serviceIds.Where(sid => !existing.Contains(sid)))
        {
            veterinarian.VeterinarianServices.Add(new VeterinarianService { VeterinarianId = id, ServiceId = serviceId });
        }

        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success("Veteriner bilgileri güncellendi.");
    }

    public async Task<ServiceResult<DeleteOutcome>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsAdmin)
        {
            return ServiceResult<DeleteOutcome>.Forbidden();
        }

        var veterinarian = await Veterinarians.Query().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (veterinarian is null)
        {
            return ServiceResult<DeleteOutcome>.NotFound("Veteriner bulunamadı.");
        }

        var hasHistory = await Veterinarians.QueryNoTracking()
            .AnyAsync(v => v.Id == id && (v.Appointments.Any() || v.ExaminationNotes.Any() || v.Vaccinations.Any()), cancellationToken);

        if (hasHistory)
        {
            // Randevu, muayene notu veya aşı geçmişi olan hekim silinmez; arşivlenir ve yeni randevu alamaz.
            veterinarian.IsActive = false;
            await UnitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult<DeleteOutcome>.Success(DeleteOutcome.Archived,
                "Hekimin geçmiş kayıtları olduğu için silinmedi, pasif yapıldı. Artık yeni randevu alamaz.");
        }

        await using var transaction = await UnitOfWork.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);

        var userId = veterinarian.UserId;
        Veterinarians.Remove(veterinarian); // Müsaitlik ve hizmet eşleşmeleri Cascade ile silinir.
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        // Hekimin giriş hesabı: hayvan sahibi olarak bir kaydı yoksa silinir, varsa sadece rolü kaldırılır.
        var user = await _userManager.FindByIdAsync(userId);
        if (user is not null)
        {
            var hasOwnRecords = await Repo<Pet>().QueryNoTracking().AnyAsync(p => p.OwnerId == userId, cancellationToken)
                || await Repo<Appointment>().QueryNoTracking().AnyAsync(a => a.OwnerId == userId, cancellationToken);

            if (hasOwnRecords)
            {
                await _userManager.RemoveFromRoleAsync(user, RoleNames.Veterinarian);
            }
            else
            {
                await _userManager.DeleteAsync(user);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return ServiceResult<DeleteOutcome>.Success(DeleteOutcome.Deleted, "Veteriner ve giriş hesabı silindi.");
    }

    /// <summary>Seçilen hizmetlerden sadece hekimin kliniğine ait ve aktif olanları bırakır.</summary>
    private async Task<List<int>> FilterServicesOfClinicAsync(IEnumerable<int> serviceIds, int clinicId, CancellationToken cancellationToken)
    {
        var ids = serviceIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return ids;
        }

        return await Repo<Service>().QueryNoTracking()
            .Where(s => ids.Contains(s.Id) && s.ClinicId == clinicId && s.IsActive)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);
    }
}
