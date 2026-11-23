using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Business.Rules;
using VeterinerKlinigi.Data.Repositories;
using VeterinerKlinigi.Entities;

namespace VeterinerKlinigi.Business.Concrete;

public class AvailabilityManager : ManagerBase, IAvailabilityService
{
    public AvailabilityManager(IUnitOfWork unitOfWork, ICurrentUserService currentUser, TimeProvider timeProvider)
        : base(unitOfWork, currentUser, timeProvider)
    {
    }

    private IRepository<VeterinarianAvailability> Availabilities => Repo<VeterinarianAvailability>();

    public async Task<List<AvailabilityDto>> GetByVeterinarianAsync(int veterinarianId, CancellationToken cancellationToken = default)
    {
        var list = await Availabilities.QueryNoTracking()
            .Where(a => a.VeterinarianId == veterinarianId)
            .Select(Projections.Availability)
            .ToListAsync(cancellationToken);

        return list
            .OrderBy(a => ((int)a.DayOfWeek + 6) % 7) // Pazartesi ilk sırada
            .ThenBy(a => a.StartTime)
            .ToList();
    }

    public async Task<ServiceResult<AvailabilitySaveDto>> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var availability = await FindManageableAsync(id, cancellationToken);
        if (availability is null)
        {
            return ServiceResult<AvailabilitySaveDto>.NotFound("Müsaitlik kaydı bulunamadı.");
        }

        return ServiceResult<AvailabilitySaveDto>.Success(new AvailabilitySaveDto
        {
            VeterinarianId = availability.VeterinarianId,
            DayOfWeek = availability.DayOfWeek,
            StartTime = availability.StartTime,
            EndTime = availability.EndTime
        });
    }

    public async Task<ServiceResult<int>> CreateAsync(AvailabilitySaveDto dto, CancellationToken cancellationToken = default)
    {
        ApplyOwnVeterinarian(dto);

        var check = await ValidateAsync(null, dto, cancellationToken);
        if (!check.IsSuccess)
        {
            return ServiceResult<int>.FromError(check);
        }

        var availability = new VeterinarianAvailability
        {
            VeterinarianId = dto.VeterinarianId,
            DayOfWeek = dto.DayOfWeek,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime
        };

        await Availabilities.AddAsync(availability, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<int>.Success(availability.Id, "Müsaitlik aralığı eklendi.");
    }

    public async Task<ServiceResult> UpdateAsync(int id, AvailabilitySaveDto dto, CancellationToken cancellationToken = default)
    {
        var availability = await FindManageableAsync(id, cancellationToken);
        if (availability is null)
        {
            return ServiceResult.NotFound("Müsaitlik kaydı bulunamadı.");
        }

        // Bir aralık başka bir hekime taşınamaz.
        dto.VeterinarianId = availability.VeterinarianId;

        var check = await ValidateAsync(id, dto, cancellationToken);
        if (!check.IsSuccess)
        {
            return check;
        }

        availability.DayOfWeek = dto.DayOfWeek;
        availability.StartTime = dto.StartTime;
        availability.EndTime = dto.EndTime;
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success("Müsaitlik aralığı güncellendi.");
    }

    public async Task<ServiceResult<int>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var availability = await FindManageableAsync(id, cancellationToken);
        if (availability is null)
        {
            return ServiceResult<int>.NotFound("Müsaitlik kaydı bulunamadı.");
        }

        // Not: Bu aralıkta önceden alınmış randevular silinmez/iptal edilmez; sadece yeni randevu alınamaz.
        var veterinarianId = availability.VeterinarianId;
        Availabilities.Remove(availability);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<int>.Success(veterinarianId, "Müsaitlik aralığı silindi.");
    }

    /// <summary>Veteriner kendi adına işlem yapıyorsa, formdaki hekim Id'si yok sayılır ve kendi Id'si kullanılır.</summary>
    private void ApplyOwnVeterinarian(AvailabilitySaveDto dto)
    {
        if (!CurrentUser.IsAdmin && CurrentUser.VeterinarianId is int ownId)
        {
            dto.VeterinarianId = ownId;
        }
    }

    /// <summary>Admin her kaydı, veteriner sadece kendi kayıtlarını yönetebilir.</summary>
    private async Task<VeterinarianAvailability?> FindManageableAsync(int id, CancellationToken cancellationToken)
    {
        var query = Availabilities.Query().Where(a => a.Id == id);

        if (!CurrentUser.IsAdmin)
        {
            if (CurrentUser.VeterinarianId is not int ownId)
            {
                return null;
            }

            query = query.Where(a => a.VeterinarianId == ownId);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<ServiceResult> ValidateAsync(int? id, AvailabilitySaveDto dto, CancellationToken cancellationToken)
    {
        if (!CurrentUser.IsAdmin && CurrentUser.VeterinarianId != dto.VeterinarianId)
        {
            return ServiceResult.Forbidden();
        }

        if (!Enum.IsDefined(dto.DayOfWeek))
        {
            return ServiceResult.Invalid("Geçerli bir gün seçin.", nameof(dto.DayOfWeek));
        }

        if (dto.StartTime >= dto.EndTime)
        {
            return ServiceResult.Invalid("Bitiş saati başlangıç saatinden sonra olmalıdır.", nameof(dto.EndTime));
        }

        var clinic = await Repo<Veterinarian>().QueryNoTracking()
            .Where(v => v.Id == dto.VeterinarianId)
            .Select(v => new { v.Clinic.OpeningTime, v.Clinic.ClosingTime, v.Clinic.Name })
            .FirstOrDefaultAsync(cancellationToken);
        if (clinic is null)
        {
            return ServiceResult.Invalid("Veteriner bulunamadı.", nameof(dto.VeterinarianId));
        }

        // Müsaitlik, kliniğin çalışma saatleri içinde olmalı.
        if (!AppointmentRules.FitsInWindow(dto.StartTime, dto.EndTime, clinic.OpeningTime, clinic.ClosingTime))
        {
            return ServiceResult.Invalid(
                $"Müsaitlik, kliniğin çalışma saatleri ({clinic.OpeningTime:HH\\:mm} - {clinic.ClosingTime:HH\\:mm}) içinde olmalıdır.",
                nameof(dto.StartTime));
        }

        // Aynı hekimin aynı gündeki diğer aralıklarıyla çakışmamalı.
        var sameDay = await Availabilities.QueryNoTracking()
            .Where(a => a.VeterinarianId == dto.VeterinarianId && a.DayOfWeek == dto.DayOfWeek && a.Id != id)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync(cancellationToken);

        var overlapping = sameDay.FirstOrDefault(a => dto.StartTime < a.EndTime && a.StartTime < dto.EndTime);
        if (overlapping is not null)
        {
            return ServiceResult.Conflict(
                $"{DisplayText.Day(dto.DayOfWeek)} günü {overlapping.StartTime:HH\\:mm} - {overlapping.EndTime:HH\\:mm} aralığıyla çakışıyor.",
                nameof(dto.StartTime));
        }

        return ServiceResult.Success();
    }
}
