using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Business.Security;
using VeterinerKlinigi.Data.Repositories;
using VeterinerKlinigi.Entities;

namespace VeterinerKlinigi.Business.Concrete;

public class VaccinationManager : ManagerBase, IVaccinationService
{
    public VaccinationManager(IUnitOfWork unitOfWork, ICurrentUserService currentUser, TimeProvider timeProvider)
        : base(unitOfWork, currentUser, timeProvider)
    {
    }

    private IRepository<Vaccination> Vaccinations => Repo<Vaccination>();

    public async Task<List<VaccinationScheduleDto>> GetSchedulesAsync(CancellationToken cancellationToken = default)
    {
        var pets = await Repo<Pet>().QueryNoTracking()
            .VisibleTo(CurrentUser)
            .Where(p => p.IsActive)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name, p.Species })
            .ToListAsync(cancellationToken);

        var petIds = pets.Select(p => p.Id).ToList();
        var vaccinations = await LoadForPetsAsync(petIds, cancellationToken);

        return pets
            .Select(p => BuildSchedule(p.Id, p.Name, DisplayText.Species(p.Species), vaccinations.Where(v => v.PetId == p.Id)))
            .ToList();
    }

    public async Task<ServiceResult<VaccinationScheduleDto>> GetScheduleByPetAsync(int petId, CancellationToken cancellationToken = default)
    {
        var pet = await Repo<Pet>().QueryNoTracking()
            .VisibleTo(CurrentUser)
            .Where(p => p.Id == petId)
            .Select(p => new { p.Id, p.Name, p.Species })
            .FirstOrDefaultAsync(cancellationToken);

        if (pet is null)
        {
            return ServiceResult<VaccinationScheduleDto>.NotFound("Evcil hayvan bulunamadı.");
        }

        var vaccinations = await LoadForPetsAsync([pet.Id], cancellationToken);
        return ServiceResult<VaccinationScheduleDto>.Success(
            BuildSchedule(pet.Id, pet.Name, DisplayText.Species(pet.Species), vaccinations));
    }

    public async Task<ServiceResult<List<VaccinationScheduleDto>>> GetScheduleByPetNameAsync(string petName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(petName))
        {
            return ServiceResult<List<VaccinationScheduleDto>>.Invalid("Hayvan adı boş olamaz.");
        }

        var name = petName.Trim();

        // VisibleTo: Üye sadece KENDİ hayvanları arasında arar. Başka bir üyenin aynı adlı
        // hayvanı (ör. başka bir "Pamuk") asla sonuçlara gelmez.
        // SQL Server'ın varsayılan karşılaştırması büyük/küçük harf duyarsızdır ("pamuk" = "Pamuk").
        var pets = await Repo<Pet>().QueryNoTracking()
            .VisibleTo(CurrentUser)
            .Where(p => p.IsActive && p.Name == name)
            .Select(p => new { p.Id, p.Name, p.Species })
            .ToListAsync(cancellationToken);

        if (pets.Count == 0)
        {
            return ServiceResult<List<VaccinationScheduleDto>>.NotFound($"'{name}' adında bir evcil hayvanınız bulunamadı.");
        }

        var vaccinations = await LoadForPetsAsync(pets.Select(p => p.Id).ToList(), cancellationToken);
        var schedules = pets
            .Select(p => BuildSchedule(p.Id, p.Name, DisplayText.Species(p.Species), vaccinations.Where(v => v.PetId == p.Id)))
            .ToList();

        return ServiceResult<List<VaccinationScheduleDto>>.Success(schedules);
    }

    public async Task<List<VaccinationDto>> GetDueAsync(int days = 30, CancellationToken cancellationToken = default)
    {
        var petIds = await Repo<Pet>().QueryNoTracking()
            .VisibleTo(CurrentUser)
            .Where(p => p.IsActive)
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var all = await LoadForPetsAsync(petIds, cancellationToken);
        return all
            .Where(v => v.DueState == VaccinationDueState.Overdue
                || (v.DueState == VaccinationDueState.Upcoming && v.DaysUntilNextDose <= days))
            .OrderBy(v => v.NextDoseDate)
            .ToList();
    }

    public async Task<ServiceResult<VaccinationDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var vaccination = await Vaccinations.QueryNoTracking()
            .VisibleTo(CurrentUser)
            .Where(v => v.Id == id)
            .Select(Projections.Vaccination)
            .FirstOrDefaultAsync(cancellationToken);

        return vaccination is null
            ? ServiceResult<VaccinationDto>.NotFound("Aşı kaydı bulunamadı.")
            : ServiceResult<VaccinationDto>.Success(vaccination);
    }

    public async Task<ServiceResult<VaccinationSaveDto>> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!CanManage())
        {
            return ServiceResult<VaccinationSaveDto>.Forbidden("Aşı kayıtlarını sadece veteriner veya admin düzenleyebilir.");
        }

        var dto = await Vaccinations.QueryNoTracking()
            .VisibleTo(CurrentUser)
            .Where(v => v.Id == id)
            .Select(v => new VaccinationSaveDto
            {
                PetId = v.PetId,
                VaccineName = v.VaccineName,
                VaccinationDate = v.VaccinationDate,
                NextDoseDate = v.NextDoseDate,
                Description = v.Description,
                VeterinarianId = v.VeterinarianId
            })
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? ServiceResult<VaccinationSaveDto>.NotFound("Aşı kaydı bulunamadı.")
            : ServiceResult<VaccinationSaveDto>.Success(dto);
    }

    public async Task<ServiceResult<int>> CreateAsync(VaccinationSaveDto dto, CancellationToken cancellationToken = default)
    {
        var check = await ValidateAsync(dto, cancellationToken);
        if (!check.IsSuccess)
        {
            return ServiceResult<int>.FromError(check);
        }

        var vaccination = new Vaccination { PetId = dto.PetId, CreatedAt = Now };
        Map(dto, vaccination);

        await Vaccinations.AddAsync(vaccination, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<int>.Success(vaccination.Id, "Aşı kaydı eklendi.");
    }

    public async Task<ServiceResult> UpdateAsync(int id, VaccinationSaveDto dto, CancellationToken cancellationToken = default)
    {
        if (!CanManage())
        {
            return ServiceResult.Forbidden("Aşı kayıtlarını sadece veteriner veya admin düzenleyebilir.");
        }

        var vaccination = await Vaccinations.Query()
            .VisibleTo(CurrentUser)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vaccination is null)
        {
            return ServiceResult.NotFound("Aşı kaydı bulunamadı.");
        }

        dto.PetId = vaccination.PetId; // Kayıt başka bir hayvana taşınamaz.
        var check = await ValidateAsync(dto, cancellationToken);
        if (!check.IsSuccess)
        {
            return check;
        }

        Map(dto, vaccination);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success("Aşı kaydı güncellendi.");
    }

    public async Task<ServiceResult<int>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!CanManage())
        {
            return ServiceResult<int>.Forbidden("Aşı kayıtlarını sadece veteriner veya admin silebilir.");
        }

        var vaccination = await Vaccinations.Query()
            .VisibleTo(CurrentUser)
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        if (vaccination is null)
        {
            return ServiceResult<int>.NotFound("Aşı kaydı bulunamadı.");
        }

        var petId = vaccination.PetId;
        Vaccinations.Remove(vaccination);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<int>.Success(petId, "Aşı kaydı silindi.");
    }

    /// <summary>Aşı kaydı yazma yetkisi: Admin veya Veteriner (üyeler sadece görüntüler).</summary>
    private bool CanManage() => CurrentUser.IsAdmin || (CurrentUser.IsVeterinarian && CurrentUser.VeterinarianId is not null);

    private async Task<ServiceResult> ValidateAsync(VaccinationSaveDto dto, CancellationToken cancellationToken)
    {
        if (!CanManage())
        {
            return ServiceResult.Forbidden("Aşı kayıtlarını sadece veteriner veya admin ekleyebilir.");
        }

        // Veteriner sadece kendisiyle randevusu olan hayvanlara aşı kaydı girebilir (VisibleTo).
        var petExists = await Repo<Pet>().QueryNoTracking()
            .VisibleTo(CurrentUser)
            .AnyAsync(p => p.Id == dto.PetId && p.IsActive, cancellationToken);
        if (!petExists)
        {
            return ServiceResult.Invalid("Evcil hayvan bulunamadı veya bu hayvana erişiminiz yok.", nameof(dto.PetId));
        }

        if (dto.VaccinationDate > Today)
        {
            return ServiceResult.Invalid("Uygulama tarihi gelecekte olamaz.", nameof(dto.VaccinationDate));
        }

        if (dto.NextDoseDate is DateOnly next && next <= dto.VaccinationDate)
        {
            return ServiceResult.Invalid("Sonraki doz tarihi uygulama tarihinden sonra olmalıdır.", nameof(dto.NextDoseDate));
        }

        // Veteriner kaydediyorsa uygulayan hekim otomatik olarak kendisidir.
        if (!CurrentUser.IsAdmin)
        {
            dto.VeterinarianId = CurrentUser.VeterinarianId;
        }
        else if (dto.VeterinarianId is int veterinarianId
            && !await Repo<Veterinarian>().QueryNoTracking().AnyAsync(v => v.Id == veterinarianId, cancellationToken))
        {
            return ServiceResult.Invalid("Seçilen hekim bulunamadı.", nameof(dto.VeterinarianId));
        }

        return ServiceResult.Success();
    }

    private static void Map(VaccinationSaveDto dto, Vaccination vaccination)
    {
        vaccination.VaccineName = dto.VaccineName.Trim();
        vaccination.VaccinationDate = dto.VaccinationDate;
        vaccination.NextDoseDate = dto.NextDoseDate;
        vaccination.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
        vaccination.VeterinarianId = dto.VeterinarianId;
    }

    private async Task<List<VaccinationDto>> LoadForPetsAsync(List<int> petIds, CancellationToken cancellationToken)
    {
        if (petIds.Count == 0)
        {
            return [];
        }

        var list = await Vaccinations.QueryNoTracking()
            .Where(v => petIds.Contains(v.PetId))
            .OrderByDescending(v => v.VaccinationDate)
            .Select(Projections.Vaccination)
            .ToListAsync(cancellationToken);

        return Projections.WithDueStates(list, Today);
    }

    private static VaccinationScheduleDto BuildSchedule(int petId, string petName, string speciesName, IEnumerable<VaccinationDto> vaccinations)
    {
        var list = vaccinations.ToList();
        return new VaccinationScheduleDto(
            petId,
            petName,
            speciesName,
            History: list.OrderByDescending(v => v.VaccinationDate).ToList(),
            Upcoming: list.Where(v => v.DueState is VaccinationDueState.Upcoming or VaccinationDueState.Planned)
                .OrderBy(v => v.NextDoseDate).ToList(),
            Overdue: list.Where(v => v.DueState == VaccinationDueState.Overdue)
                .OrderBy(v => v.NextDoseDate).ToList());
    }
}
