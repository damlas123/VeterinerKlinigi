using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Data.Repositories;
using VeterinerKlinigi.Entities;

namespace VeterinerKlinigi.Business.Concrete;

public class ClinicManager : ManagerBase, IClinicService
{
    public ClinicManager(IUnitOfWork unitOfWork, ICurrentUserService currentUser, TimeProvider timeProvider)
        : base(unitOfWork, currentUser, timeProvider)
    {
    }

    private IRepository<Clinic> Clinics => Repo<Clinic>();

    public async Task<List<ClinicDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        return await Clinics.QueryNoTracking()
            .Where(c => includeInactive || c.IsActive)
            .OrderBy(c => c.Name)
            .Select(Projections.Clinic)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<ClinicDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var isAdmin = CurrentUser.IsAdmin;
        var clinic = await Clinics.QueryNoTracking()
            .Where(c => c.Id == id && (c.IsActive || isAdmin))
            .Select(Projections.Clinic)
            .FirstOrDefaultAsync(cancellationToken);

        return clinic is null
            ? ServiceResult<ClinicDto>.NotFound("Klinik bulunamadı.")
            : ServiceResult<ClinicDto>.Success(clinic);
    }

    public async Task<ServiceResult<ClinicSaveDto>> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsAdmin)
        {
            return ServiceResult<ClinicSaveDto>.Forbidden();
        }

        var dto = await Clinics.QueryNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new ClinicSaveDto
            {
                Name = c.Name,
                Address = c.Address,
                Phone = c.Phone,
                Email = c.Email,
                Description = c.Description,
                OpeningTime = c.OpeningTime,
                ClosingTime = c.ClosingTime,
                IsActive = c.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? ServiceResult<ClinicSaveDto>.NotFound("Klinik bulunamadı.")
            : ServiceResult<ClinicSaveDto>.Success(dto);
    }

    public async Task<ServiceResult<int>> CreateAsync(ClinicSaveDto dto, CancellationToken cancellationToken = default)
    {
        var check = Validate(dto);
        if (!check.IsSuccess)
        {
            return ServiceResult<int>.FromError(check);
        }

        var clinic = new Clinic();
        Map(dto, clinic);
        await Clinics.AddAsync(clinic, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<int>.Success(clinic.Id, "Klinik eklendi.");
    }

    public async Task<ServiceResult> UpdateAsync(int id, ClinicSaveDto dto, CancellationToken cancellationToken = default)
    {
        var check = Validate(dto);
        if (!check.IsSuccess)
        {
            return check;
        }

        var clinic = await Clinics.Query().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (clinic is null)
        {
            return ServiceResult.NotFound("Klinik bulunamadı.");
        }

        Map(dto, clinic);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success("Klinik bilgileri güncellendi.");
    }

    public async Task<ServiceResult<DeleteOutcome>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsAdmin)
        {
            return ServiceResult<DeleteOutcome>.Forbidden();
        }

        var clinic = await Clinics.Query().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (clinic is null)
        {
            return ServiceResult<DeleteOutcome>.NotFound("Klinik bulunamadı.");
        }

        // Kliniğe bağlı hekim veya hizmet varsa (geçmiş randevular bunlara bağlıdır) klinik silinmez, arşivlenir.
        var hasRelations = await Clinics.QueryNoTracking()
            .AnyAsync(c => c.Id == id && (c.Veterinarians.Any() || c.Services.Any()), cancellationToken);

        if (hasRelations)
        {
            clinic.IsActive = false;
            await UnitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult<DeleteOutcome>.Success(DeleteOutcome.Archived,
                "Kliniğe bağlı hekim/hizmet kayıtları olduğu için klinik silinmedi, pasif yapıldı.");
        }

        Clinics.Remove(clinic);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<DeleteOutcome>.Success(DeleteOutcome.Deleted, "Klinik silindi.");
    }

    private ServiceResult Validate(ClinicSaveDto dto)
    {
        if (!CurrentUser.IsAdmin)
        {
            return ServiceResult.Forbidden();
        }

        if (dto.OpeningTime >= dto.ClosingTime)
        {
            return ServiceResult.Invalid("Kapanış saati açılış saatinden sonra olmalıdır.", nameof(dto.ClosingTime));
        }

        return ServiceResult.Success();
    }

    private static void Map(ClinicSaveDto dto, Clinic clinic)
    {
        clinic.Name = dto.Name.Trim();
        clinic.Address = dto.Address.Trim();
        clinic.Phone = dto.Phone.Trim();
        clinic.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        clinic.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        clinic.OpeningTime = dto.OpeningTime;
        clinic.ClosingTime = dto.ClosingTime;
        clinic.IsActive = dto.IsActive;
    }
}
