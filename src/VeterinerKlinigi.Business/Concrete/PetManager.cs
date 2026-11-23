using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Business.Security;
using VeterinerKlinigi.Data.Repositories;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Enums;

namespace VeterinerKlinigi.Business.Concrete;

public class PetManager : ManagerBase, IPetService
{
    public PetManager(IUnitOfWork unitOfWork, ICurrentUserService currentUser, TimeProvider timeProvider)
        : base(unitOfWork, currentUser, timeProvider)
    {
    }

    private IRepository<Pet> Pets => Repo<Pet>();

    public async Task<List<PetDto>> GetListAsync(PetFilter? filter = null, CancellationToken cancellationToken = default)
    {
        filter ??= new PetFilter();

        // Sahiplik filtresi: Üye sadece kendi hayvanlarını, veteriner hastalarını, admin tümünü görür.
        var query = Pets.QueryNoTracking().VisibleTo(CurrentUser);

        if (!filter.IncludeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (CurrentUser.IsAdmin && !string.IsNullOrWhiteSpace(filter.OwnerId))
        {
            query = query.Where(p => p.OwnerId == filter.OwnerId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(p => p.Name.Contains(search)
                || (p.Breed != null && p.Breed.Contains(search))
                || (p.Owner.FirstName + " " + p.Owner.LastName).Contains(search));
        }

        return await query
            .OrderBy(p => p.Name)
            .Select(Projections.Pet)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<PetDetailDto>> GetDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var pet = await Pets.QueryNoTracking()
            .VisibleTo(CurrentUser)
            .Where(p => p.Id == id)
            .Select(p => new
            {
                Pet = new PetDto(p.Id, p.Name, p.Species, p.Breed, p.BirthDate, p.Weight, p.PhotoUrl, p.IsActive,
                    p.OwnerId, p.Owner.FirstName + " " + p.Owner.LastName),
                OwnerEmail = p.Owner.Email,
                OwnerPhone = p.Owner.PhoneNumber
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (pet is null)
        {
            // Hayvan yok VEYA kullanıcının değil: ikisinde de aynı cevap (id tahmin etmeyi zorlaştırır).
            return ServiceResult<PetDetailDto>.NotFound("Evcil hayvan bulunamadı.");
        }

        // Hayvana erişimi olan kullanıcı, hayvanın TÜM sağlık geçmişini görür.
        var vaccinations = await Repo<Vaccination>().QueryNoTracking()
            .Where(v => v.PetId == id)
            .OrderByDescending(v => v.VaccinationDate)
            .Select(Projections.Vaccination)
            .ToListAsync(cancellationToken);

        var appointments = await Repo<Appointment>().QueryNoTracking()
            .Where(a => a.PetId == id)
            .OrderByDescending(a => a.AppointmentDate)
            .Select(Projections.Appointment)
            .ToListAsync(cancellationToken);

        var notes = await Repo<ExaminationNote>().QueryNoTracking()
            .Where(n => n.Appointment.PetId == id)
            .OrderByDescending(n => n.Appointment.AppointmentDate)
            .Select(Projections.ExaminationNote)
            .ToListAsync(cancellationToken);

        var canEdit = CurrentUser.IsAdmin || (CurrentUser.IsMember && pet.Pet.OwnerId == CurrentUser.UserId);
        var canManageVaccinations = CurrentUser.IsAdmin || CurrentUser.IsVeterinarian;

        return ServiceResult<PetDetailDto>.Success(new PetDetailDto(
            pet.Pet,
            pet.OwnerEmail ?? string.Empty,
            pet.OwnerPhone,
            Projections.WithDueStates(vaccinations, Today),
            appointments,
            notes,
            canEdit,
            canManageVaccinations));
    }

    public async Task<ServiceResult<PetEditDto>> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var pet = await Pets.QueryNoTracking()
            .ManageableBy(CurrentUser)
            .Where(p => p.Id == id && p.IsActive)
            .Select(p => new PetEditDto(
                new PetSaveDto
                {
                    Name = p.Name,
                    Species = p.Species,
                    Breed = p.Breed,
                    BirthDate = p.BirthDate,
                    Weight = p.Weight
                },
                p.PhotoUrl))
            .FirstOrDefaultAsync(cancellationToken);

        return pet is null
            ? ServiceResult<PetEditDto>.NotFound("Evcil hayvan bulunamadı.")
            : ServiceResult<PetEditDto>.Success(pet);
    }

    public async Task<ServiceResult<int>> CreateAsync(PetSaveDto dto, string? photoUrl, CancellationToken cancellationToken = default)
    {
        // Evcil hayvanı sahibi (Üye) ekler; hayvan her zaman giriş yapan kullanıcıya bağlanır.
        if (!CurrentUser.IsMember || CurrentUser.UserId is not string ownerId)
        {
            return ServiceResult<int>.Forbidden("Evcil hayvan sadece üye hesabıyla eklenebilir.");
        }

        var check = Validate(dto);
        if (!check.IsSuccess)
        {
            return ServiceResult<int>.FromError(check);
        }

        var pet = new Pet
        {
            OwnerId = ownerId,
            PhotoUrl = photoUrl,
            CreatedAt = Now
        };
        Map(dto, pet);

        await Pets.AddAsync(pet, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<int>.Success(pet.Id, $"{pet.Name} eklendi.");
    }

    public async Task<ServiceResult<string?>> UpdateAsync(int id, PetSaveDto dto, string? newPhotoUrl, bool removePhoto, CancellationToken cancellationToken = default)
    {
        var pet = await Pets.Query()
            .ManageableBy(CurrentUser)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
        if (pet is null)
        {
            return ServiceResult<string?>.NotFound("Evcil hayvan bulunamadı.");
        }

        var check = Validate(dto);
        if (!check.IsSuccess)
        {
            return ServiceResult<string?>.FromError(check);
        }

        Map(dto, pet);

        // Fotoğraf değiştiyse veya kaldırıldıysa eski dosyanın yolunu döndürürüz; Web katmanı diskten siler.
        string? obsoletePhoto = null;
        if (newPhotoUrl is not null)
        {
            obsoletePhoto = pet.PhotoUrl;
            pet.PhotoUrl = newPhotoUrl;
        }
        else if (removePhoto)
        {
            obsoletePhoto = pet.PhotoUrl;
            pet.PhotoUrl = null;
        }

        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<string?>.Success(obsoletePhoto, $"{pet.Name} güncellendi.");
    }

    public async Task<ServiceResult<PetDeleteResult>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var pet = await Pets.Query()
            .ManageableBy(CurrentUser)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive, cancellationToken);
        if (pet is null)
        {
            return ServiceResult<PetDeleteResult>.NotFound("Evcil hayvan bulunamadı.");
        }

        var hasHistory = await Pets.QueryNoTracking()
            .AnyAsync(p => p.Id == id && (p.Appointments.Any() || p.Vaccinations.Any()), cancellationToken);

        if (hasHistory)
        {
            // Sağlık geçmişi korunur: hayvan silinmez, arşivlenir.
            pet.IsActive = false;

            // Arşivlenen hayvanın ileri tarihli aktif randevuları iptal edilir (hekimin takviminde yer tutmasın).
            var now = Now;
            var futureAppointments = await Repo<Appointment>().Query()
                .Where(a => a.PetId == id && a.AppointmentDate > now
                    && (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Approved))
                .ToListAsync(cancellationToken);
            foreach (var appointment in futureAppointments)
            {
                appointment.Status = AppointmentStatus.Cancelled;
            }

            await UnitOfWork.SaveChangesAsync(cancellationToken);

            var message = futureAppointments.Count > 0
                ? $"{pet.Name} arşivlendi; sağlık geçmişi korundu ve {futureAppointments.Count} ileri tarihli randevusu iptal edildi."
                : $"{pet.Name} arşivlendi; aşı ve randevu geçmişi korundu.";
            return ServiceResult<PetDeleteResult>.Success(new PetDeleteResult(DeleteOutcome.Archived, null), message);
        }

        var photoUrl = pet.PhotoUrl;
        Pets.Remove(pet);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult<PetDeleteResult>.Success(new PetDeleteResult(DeleteOutcome.Deleted, photoUrl), $"{pet.Name} silindi.");
    }

    private ServiceResult Validate(PetSaveDto dto)
    {
        if (!Enum.IsDefined(dto.Species))
        {
            return ServiceResult.Invalid("Geçerli bir tür seçin.", nameof(dto.Species));
        }

        if (dto.BirthDate > Today)
        {
            return ServiceResult.Invalid("Doğum tarihi gelecekte olamaz.", nameof(dto.BirthDate));
        }

        if (dto.BirthDate < Today.AddYears(-50))
        {
            return ServiceResult.Invalid("Doğum tarihi 50 yıldan eski olamaz.", nameof(dto.BirthDate));
        }

        if (dto.Weight <= 0 || dto.Weight > 150)
        {
            return ServiceResult.Invalid("Kilo 0,01 ile 150 kg arasında olmalıdır.", nameof(dto.Weight));
        }

        return ServiceResult.Success();
    }

    private static void Map(PetSaveDto dto, Pet pet)
    {
        pet.Name = dto.Name.Trim();
        pet.Species = dto.Species;
        pet.Breed = string.IsNullOrWhiteSpace(dto.Breed) ? null : dto.Breed.Trim();
        pet.BirthDate = dto.BirthDate;
        pet.Weight = Math.Round(dto.Weight, 2);
    }
}
