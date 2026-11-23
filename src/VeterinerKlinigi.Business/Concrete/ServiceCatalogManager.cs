using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Data.Repositories;
using VeterinerKlinigi.Entities;

namespace VeterinerKlinigi.Business.Concrete;

public class ServiceCatalogManager : ManagerBase, IServiceCatalogService
{
    public ServiceCatalogManager(IUnitOfWork unitOfWork, ICurrentUserService currentUser, TimeProvider timeProvider)
        : base(unitOfWork, currentUser, timeProvider)
    {
    }

    private IRepository<Service> Services => Repo<Service>();

    public async Task<List<ServiceDto>> GetAllAsync(int? clinicId = null, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        // includeInactive sadece Admin için anlamlıdır; diğer kullanıcılar arşivlenmiş hizmetleri görmez.
        var showInactive = includeInactive && CurrentUser.IsAdmin;

        return await Services.QueryNoTracking()
            .Where(s => showInactive || (s.IsActive && s.Clinic.IsActive))
            .Where(s => clinicId == null || s.ClinicId == clinicId)
            .OrderBy(s => s.Clinic.Name).ThenBy(s => s.Name)
            .Select(Projections.Service)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<ServiceDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var isAdmin = CurrentUser.IsAdmin;
        var service = await Services.QueryNoTracking()
            .Where(s => s.Id == id && (s.IsActive || isAdmin))
            .Select(Projections.Service)
            .FirstOrDefaultAsync(cancellationToken);

        return service is null
            ? ServiceResult<ServiceDto>.NotFound("Hizmet bulunamadı.")
            : ServiceResult<ServiceDto>.Success(service);
    }

    public async Task<ServiceResult<ServiceSaveDto>> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsAdmin)
        {
            return ServiceResult<ServiceSaveDto>.Forbidden();
        }

        var dto = await Services.QueryNoTracking()
            .Where(s => s.Id == id)
            .Select(s => new ServiceSaveDto
            {
                ClinicId = s.ClinicId,
                Name = s.Name,
                Description = s.Description,
                Duration = s.Duration,
                Price = s.Price,
                IsActive = s.IsActive
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? ServiceResult<ServiceSaveDto>.NotFound("Hizmet bulunamadı.")
            : ServiceResult<ServiceSaveDto>.Success(dto);
    }

    public async Task<ServiceResult<int>> CreateAsync(ServiceSaveDto dto, CancellationToken cancellationToken = default)
    {
        var check = await ValidateAsync(null, dto, cancellationToken);
        if (!check.IsSuccess)
        {
            return ServiceResult<int>.FromError(check);
        }

        var service = new Service();
        Map(dto, service);
        await Services.AddAsync(service, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<int>.Success(service.Id, "Hizmet eklendi.");
    }

    public async Task<ServiceResult> UpdateAsync(int id, ServiceSaveDto dto, CancellationToken cancellationToken = default)
    {
        var check = await ValidateAsync(id, dto, cancellationToken);
        if (!check.IsSuccess)
        {
            return check;
        }

        var service = await Services.Query()
            .Include(s => s.VeterinarianServices).ThenInclude(vs => vs.Veterinarian)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (service is null)
        {
            return ServiceResult.NotFound("Hizmet bulunamadı.");
        }

        // Hizmet başka bir kliniğe taşındıysa, eski kliniğin hekimleriyle olan eşleşmeleri kaldırılır
        // (bir hekim sadece kendi kliniğinin hizmetlerini verebilir).
        if (service.ClinicId != dto.ClinicId)
        {
            var invalidLinks = service.VeterinarianServices.Where(vs => vs.Veterinarian.ClinicId != dto.ClinicId).ToList();
            Repo<VeterinarianService>().RemoveRange(invalidLinks);
        }

        Map(dto, service);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success("Hizmet güncellendi.");
    }

    public async Task<ServiceResult<DeleteOutcome>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsAdmin)
        {
            return ServiceResult<DeleteOutcome>.Forbidden();
        }

        var service = await Services.Query().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (service is null)
        {
            return ServiceResult<DeleteOutcome>.NotFound("Hizmet bulunamadı.");
        }

        var hasAppointments = await Repo<Appointment>().QueryNoTracking().AnyAsync(a => a.ServiceId == id, cancellationToken);
        if (hasAppointments)
        {
            // Randevu geçmişi ve ciro raporu bozulmasın diye silmek yerine arşivliyoruz.
            service.IsActive = false;
            await UnitOfWork.SaveChangesAsync(cancellationToken);
            return ServiceResult<DeleteOutcome>.Success(DeleteOutcome.Archived,
                "Bu hizmete ait randevular olduğu için hizmet silinmedi, pasif yapıldı. Yeni randevularda listelenmeyecek.");
        }

        Services.Remove(service); // Hekim-hizmet eşleşmeleri Cascade ile birlikte silinir.
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<DeleteOutcome>.Success(DeleteOutcome.Deleted, "Hizmet silindi.");
    }

    private async Task<ServiceResult> ValidateAsync(int? id, ServiceSaveDto dto, CancellationToken cancellationToken)
    {
        if (!CurrentUser.IsAdmin)
        {
            return ServiceResult.Forbidden();
        }

        var clinicExists = await Repo<Clinic>().QueryNoTracking().AnyAsync(c => c.Id == dto.ClinicId, cancellationToken);
        if (!clinicExists)
        {
            return ServiceResult.Invalid("Seçilen klinik bulunamadı.", nameof(dto.ClinicId));
        }

        if (dto.IsActive)
        {
            var name = dto.Name.Trim();
            var duplicate = await Services.QueryNoTracking()
                .AnyAsync(s => s.ClinicId == dto.ClinicId && s.IsActive && s.Name == name && s.Id != id, cancellationToken);
            if (duplicate)
            {
                return ServiceResult.Conflict("Bu klinikte aynı isimde aktif bir hizmet zaten var.", nameof(dto.Name));
            }
        }

        return ServiceResult.Success();
    }

    private static void Map(ServiceSaveDto dto, Service service)
    {
        service.ClinicId = dto.ClinicId;
        service.Name = dto.Name.Trim();
        service.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        service.Duration = dto.Duration;
        service.Price = dto.Price;
        service.IsActive = dto.IsActive;
    }
}
