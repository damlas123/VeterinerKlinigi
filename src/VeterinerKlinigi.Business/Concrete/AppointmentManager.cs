using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Business.Options;
using VeterinerKlinigi.Business.Rules;
using VeterinerKlinigi.Business.Security;
using VeterinerKlinigi.Data.Repositories;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Enums;

namespace VeterinerKlinigi.Business.Concrete;

/// <summary>
/// RANDEVU SİSTEMİ — projenin en önemli iş kuralları.
/// MVC (Member/Appointments), REST API (POST /api/appointments) ve MCP (randevu_olustur)
/// bu sınıfı ortak kullanır; kurallar başka hiçbir yerde tekrar yazılmaz.
/// </summary>
public class AppointmentManager : ManagerBase, IAppointmentService
{
    private readonly AppointmentOptions _options;

    public AppointmentManager(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        TimeProvider timeProvider,
        IOptions<AppointmentOptions> options)
        : base(unitOfWork, currentUser, timeProvider)
    {
        _options = options.Value;
    }

    private IRepository<Appointment> Appointments => Repo<Appointment>();

    // =====================================================================
    // LİSTELEME VE DETAY
    // =====================================================================

    public async Task<List<AppointmentDto>> GetListAsync(AppointmentFilter? filter = null, CancellationToken cancellationToken = default)
    {
        filter ??= new AppointmentFilter();

        // Sahiplik filtresi: üye kendi randevularını, veteriner kendisine ait olanları, admin tümünü görür.
        var query = Appointments.QueryNoTracking().VisibleTo(CurrentUser);

        if (filter.Status is AppointmentStatus status)
        {
            query = query.Where(a => a.Status == status);
        }

        if (filter.From is DateOnly from)
        {
            var fromDate = from.ToDateTime(TimeOnly.MinValue);
            query = query.Where(a => a.AppointmentDate >= fromDate);
        }

        if (filter.To is DateOnly to)
        {
            var toDate = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(a => a.AppointmentDate < toDate);
        }

        if (filter.VeterinarianId is int veterinarianId)
        {
            query = query.Where(a => a.VeterinarianId == veterinarianId);
        }

        if (filter.PetId is int petId)
        {
            query = query.Where(a => a.PetId == petId);
        }

        if (filter.ClinicId is int clinicId)
        {
            query = query.Where(a => a.Service.ClinicId == clinicId);
        }

        return await query
            .OrderByDescending(a => a.AppointmentDate)
            .Select(Projections.Appointment)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<AppointmentDetailDto>> GetDetailAsync(int id, CancellationToken cancellationToken = default)
    {
        var data = await Appointments.QueryNoTracking()
            .VisibleTo(CurrentUser)
            .Where(a => a.Id == id)
            .Select(a => new
            {
                Appointment = new AppointmentDto(
                    a.Id, a.PetId, a.Pet.Name, a.Pet.Species, a.OwnerId, a.Owner.FirstName + " " + a.Owner.LastName,
                    a.VeterinarianId, a.Veterinarian.FullName, a.ServiceId, a.Service.Name, a.Service.Clinic.Name,
                    a.AppointmentDate, a.Duration, a.Price, a.Status, a.OwnerNote, a.RejectionReason, a.CreatedAt,
                    a.ExaminationNote != null),
                OwnerEmail = a.Owner.Email,
                OwnerPhone = a.Owner.PhoneNumber
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (data is null)
        {
            return ServiceResult<AppointmentDetailDto>.NotFound("Randevu bulunamadı.");
        }

        var note = await Repo<ExaminationNote>().QueryNoTracking()
            .Where(n => n.AppointmentId == id)
            .Select(Projections.ExaminationNote)
            .FirstOrDefaultAsync(cancellationToken);

        var a = data.Appointment;
        var now = Now;
        var isOwner = CurrentUser.UserId == a.OwnerId;
        var isAssignedVet = CurrentUser.IsVeterinarian && CurrentUser.VeterinarianId == a.VeterinarianId;
        var canDecide = CurrentUser.IsAdmin || isAssignedVet;

        var detail = new AppointmentDetailDto(
            a,
            note,
            data.OwnerEmail ?? string.Empty,
            data.OwnerPhone,
            CanApprove: canDecide && a.Status == AppointmentStatus.Pending && a.AppointmentDate > now,
            CanReject: canDecide && a.Status == AppointmentStatus.Pending,
            CanCancel: (CurrentUser.IsAdmin && a.Status is AppointmentStatus.Pending or AppointmentStatus.Approved)
                || (isOwner && a.Status is AppointmentStatus.Pending or AppointmentStatus.Approved && a.AppointmentDate > now),
            CanEdit: (CurrentUser.IsAdmin || isOwner) && a.Status == AppointmentStatus.Pending && a.AppointmentDate > now,
            CanComplete: isAssignedVet && a.Status == AppointmentStatus.Approved && a.AppointmentDate <= now && note is null,
            CanDelete: CurrentUser.IsAdmin && a.Status is AppointmentStatus.Cancelled or AppointmentStatus.Rejected && note is null);

        return ServiceResult<AppointmentDetailDto>.Success(detail);
    }

    public async Task<ServiceResult<AppointmentSaveDto>> GetForEditAsync(int id, CancellationToken cancellationToken = default)
    {
        var appointment = await FindEditableAsync(id, cancellationToken);
        if (appointment is null)
        {
            return ServiceResult<AppointmentSaveDto>.NotFound("Düzenlenebilir bir randevu bulunamadı.");
        }

        return ServiceResult<AppointmentSaveDto>.Success(new AppointmentSaveDto
        {
            PetId = appointment.PetId,
            ServiceId = appointment.ServiceId,
            VeterinarianId = appointment.VeterinarianId,
            AppointmentDate = appointment.AppointmentDate,
            OwnerNote = appointment.OwnerNote
        });
    }

    // =====================================================================
    // UYGUN SAATLER
    // =====================================================================

    public async Task<ServiceResult<List<VeterinarianSlotsDto>>> GetAvailableSlotsAsync(
        int serviceId, DateOnly date, int? veterinarianId = null, int? excludeAppointmentId = null,
        CancellationToken cancellationToken = default)
    {
        // 1) Hizmet ve kliniğinin çalışma saatleri
        var service = await Repo<Service>().QueryNoTracking()
            .Where(s => s.Id == serviceId && s.IsActive && s.Clinic.IsActive)
            .Select(s => new { s.Id, s.Name, s.Duration, s.Clinic.OpeningTime, s.Clinic.ClosingTime })
            .FirstOrDefaultAsync(cancellationToken);

        if (service is null)
        {
            return ServiceResult<List<VeterinarianSlotsDto>>.NotFound("Hizmet bulunamadı.");
        }

        if (date < Today)
        {
            return ServiceResult<List<VeterinarianSlotsDto>>.Invalid("Geçmiş bir tarih için randevu alınamaz.", "date");
        }

        if (date > Today.AddDays(_options.MaxDaysAhead))
        {
            return ServiceResult<List<VeterinarianSlotsDto>>.Invalid(
                $"En fazla {_options.MaxDaysAhead} gün sonrasına randevu alınabilir.", "date");
        }

        // 2) Bu hizmeti verebilen aktif hekimler ve o günkü müsaitlik aralıkları
        var day = date.DayOfWeek;
        var veterinarians = await Repo<Veterinarian>().QueryNoTracking()
            .Where(v => v.IsActive && v.VeterinarianServices.Any(vs => vs.ServiceId == serviceId))
            .Where(v => veterinarianId == null || v.Id == veterinarianId)
            .OrderBy(v => v.FullName)
            .Select(v => new
            {
                v.Id,
                v.FullName,
                v.Specialty,
                Windows = v.Availabilities
                    .Where(a => a.DayOfWeek == day)
                    .Select(a => new { a.StartTime, a.EndTime })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        if (veterinarianId is not null && veterinarians.Count == 0)
        {
            return ServiceResult<List<VeterinarianSlotsDto>>.Invalid("Seçilen veteriner bu hizmeti vermiyor.", "veterinarianId");
        }

        // 3) O günün dolu randevuları (TEK sorguyla, tüm hekimler için)
        var veterinarianIds = veterinarians.Select(v => v.Id).ToList();
        var dayStart = date.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);

        var busy = await Appointments.QueryNoTracking()
            .Where(a => veterinarianIds.Contains(a.VeterinarianId)
                && a.AppointmentDate >= dayStart && a.AppointmentDate < dayEnd
                && a.Status != AppointmentStatus.Rejected && a.Status != AppointmentStatus.Cancelled
                && (excludeAppointmentId == null || a.Id != excludeAppointmentId))
            .Select(a => new { a.VeterinarianId, a.AppointmentDate, a.Duration })
            .ToListAsync(cancellationToken);

        // 4) Her hekim için boş saatleri hesapla (AppointmentRules: saf fonksiyon)
        var now = Now;
        var result = veterinarians.Select(v =>
        {
            var windows = v.Windows
                .Select(w => AppointmentRules.ClipToClinicHours(w.StartTime, w.EndTime, service.OpeningTime, service.ClosingTime))
                .Where(w => w is not null)
                .Select(w => w!.Value);

            var vetBusy = busy
                .Where(b => b.VeterinarianId == v.Id)
                .Select(b => (b.AppointmentDate, b.AppointmentDate.AddMinutes(b.Duration)))
                .ToList();

            var slots = AppointmentRules.GenerateSlots(date, windows, service.Duration, _options.SlotIntervalMinutes, vetBusy, now);

            return new VeterinarianSlotsDto(v.Id, v.FullName, v.Specialty, slots.Select(s => s.ToString("HH:mm")).ToList());
        }).ToList();

        return ServiceResult<List<VeterinarianSlotsDto>>.Success(result);
    }

    // =====================================================================
    // OLUŞTURMA VE GÜNCELLEME
    // =====================================================================

    public async Task<ServiceResult<AppointmentDto>> CreateAsync(AppointmentSaveDto dto, CancellationToken cancellationToken = default)
    {
        if (CurrentUser.IsVeterinarian && !CurrentUser.IsAdmin)
        {
            return ServiceResult<AppointmentDto>.Forbidden("Veteriner hesabıyla randevu oluşturulamaz.");
        }

        try
        {
            // R-12: Kontrol + kayıt tek bir SERIALIZABLE transaction içinde yapılır.
            // Aynı anda aynı saate iki istek gelirse ikincisi, birincisi bitene kadar bekler
            // ve kontrol sırasında ilk randevuyu görür; böylece çift randevu oluşmaz.
            await using var transaction = await UnitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var validation = await ValidateAsync(dto, null, cancellationToken);
            if (!validation.IsSuccess)
            {
                return ServiceResult<AppointmentDto>.FromError(validation);
            }

            var context = validation.Data!;
            var appointment = new Appointment
            {
                PetId = dto.PetId,
                OwnerId = context.OwnerId,                 // R-13: sahibi hayvandan alınır
                VeterinarianId = dto.VeterinarianId,
                ServiceId = dto.ServiceId,
                AppointmentDate = context.Start,
                Duration = context.Duration,               // R-13: süre hizmetten kopyalanır
                Price = context.Price,                     // R-13: ücret hizmetten kopyalanır
                Status = AppointmentStatus.Pending,        // R-13: başlangıç durumu "Bekliyor"
                OwnerNote = Clean(dto.OwnerNote),
                CreatedAt = Now
            };

            await Appointments.AddAsync(appointment, cancellationToken);
            await UnitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var created = await LoadDtoAsync(appointment.Id, cancellationToken);
            return ServiceResult<AppointmentDto>.Success(created,
                $"Randevu talebiniz alındı: {created.PetName}, {created.AppointmentDate:dd.MM.yyyy HH:mm}. Onay bekleniyor.");
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            // Serializable transaction'da eşzamanlı bir işlem çakışırsa (deadlock) veritabanı hata verir.
            return ServiceResult<AppointmentDto>.Conflict("Bu saat için aynı anda başka bir işlem yapıldı. Lütfen uygun saatleri yenileyip tekrar deneyin.");
        }
    }

    public async Task<ServiceResult<AppointmentDto>> UpdateAsync(int id, AppointmentSaveDto dto, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var transaction = await UnitOfWork.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

            var appointment = await FindEditableAsync(id, cancellationToken);
            if (appointment is null)
            {
                return ServiceResult<AppointmentDto>.NotFound("Düzenlenebilir bir randevu bulunamadı. Sadece 'Bekliyor' durumundaki ileri tarihli randevular değiştirilebilir.");
            }

            // Kurallar, randevunun kendisi hariç tutularak yeniden kontrol edilir.
            var validation = await ValidateAsync(dto, id, cancellationToken);
            if (!validation.IsSuccess)
            {
                return ServiceResult<AppointmentDto>.FromError(validation);
            }

            var context = validation.Data!;
            appointment.PetId = dto.PetId;
            appointment.OwnerId = context.OwnerId;
            appointment.VeterinarianId = dto.VeterinarianId;
            appointment.ServiceId = dto.ServiceId;
            appointment.AppointmentDate = context.Start;
            appointment.Duration = context.Duration;
            appointment.Price = context.Price;
            appointment.OwnerNote = Clean(dto.OwnerNote);

            await UnitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var updated = await LoadDtoAsync(id, cancellationToken);
            return ServiceResult<AppointmentDto>.Success(updated, "Randevu güncellendi. Onay bekleniyor.");
        }
        catch (Exception ex) when (ex is DbUpdateException or DbException)
        {
            return ServiceResult<AppointmentDto>.Conflict("Bu saat için aynı anda başka bir işlem yapıldı. Lütfen tekrar deneyin.");
        }
    }

    /// <summary>
    /// RANDEVU KURALLARI (R-01 … R-11). Oluşturma ve güncelleme aynı kontrolden geçer.
    /// </summary>
    private async Task<ServiceResult<SlotContext>> ValidateAsync(AppointmentSaveDto dto, int? excludeAppointmentId, CancellationToken cancellationToken)
    {
        // R-01: Hayvan var, aktif ve kullanıcıya ait olmalı (Admin herkesin hayvanı için oluşturabilir).
        var pet = await Repo<Pet>().QueryNoTracking()
            .ManageableBy(CurrentUser)
            .Where(p => p.Id == dto.PetId && p.IsActive)
            .Select(p => new { p.Id, p.Name, p.OwnerId })
            .FirstOrDefaultAsync(cancellationToken);
        if (pet is null)
        {
            return ServiceResult<SlotContext>.NotFound("Evcil hayvan bulunamadı.");
        }

        // R-02: Hizmet var ve aktif olmalı.
        var service = await Repo<Service>().QueryNoTracking()
            .Where(s => s.Id == dto.ServiceId && s.IsActive && s.Clinic.IsActive)
            .Select(s => new { s.Id, s.Name, s.Duration, s.Price, s.ClinicId, s.Clinic.OpeningTime, s.Clinic.ClosingTime })
            .FirstOrDefaultAsync(cancellationToken);
        if (service is null)
        {
            return ServiceResult<SlotContext>.NotFound("Hizmet bulunamadı.");
        }

        // R-03: Veteriner var ve aktif olmalı.
        var veterinarian = await Repo<Veterinarian>().QueryNoTracking()
            .Where(v => v.Id == dto.VeterinarianId && v.IsActive)
            .Select(v => new
            {
                v.Id,
                v.FullName,
                // R-04: Veteriner bu hizmeti verebilmeli.
                ProvidesService = v.VeterinarianServices.Any(vs => vs.ServiceId == dto.ServiceId)
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (veterinarian is null)
        {
            return ServiceResult<SlotContext>.NotFound("Veteriner bulunamadı.");
        }

        if (!veterinarian.ProvidesService)
        {
            return ServiceResult<SlotContext>.Invalid($"{veterinarian.FullName}, '{service.Name}' hizmetini vermiyor.", nameof(dto.VeterinarianId));
        }

        var start = new DateTime(dto.AppointmentDate.Year, dto.AppointmentDate.Month, dto.AppointmentDate.Day,
            dto.AppointmentDate.Hour, dto.AppointmentDate.Minute, 0);
        var end = start.AddMinutes(service.Duration);
        var now = Now;

        // R-05: Geçmiş tarih/saat reddedilir.
        if (start <= now)
        {
            return ServiceResult<SlotContext>.Invalid("Geçmiş bir tarih veya saate randevu alınamaz.", nameof(dto.AppointmentDate));
        }

        // R-06: En fazla N gün sonrası.
        if (DateOnly.FromDateTime(start) > Today.AddDays(_options.MaxDaysAhead))
        {
            return ServiceResult<SlotContext>.Invalid($"En fazla {_options.MaxDaysAhead} gün sonrasına randevu alınabilir.", nameof(dto.AppointmentDate));
        }

        // R-07: Başlangıç saati 15 dakikalık aralıklara denk gelmeli.
        if (!AppointmentRules.IsAlignedToInterval(dto.AppointmentDate, _options.SlotIntervalMinutes))
        {
            return ServiceResult<SlotContext>.Invalid(
                $"Randevu saatleri {_options.SlotIntervalMinutes} dakikalık aralıklarla seçilebilir (ör. 10:00, 10:{_options.SlotIntervalMinutes:00}).",
                nameof(dto.AppointmentDate));
        }

        if (end.Date != start.Date)
        {
            return ServiceResult<SlotContext>.Invalid("Randevu gece yarısını geçemez.", nameof(dto.AppointmentDate));
        }

        var startTime = TimeOnly.FromDateTime(start);
        var endTime = TimeOnly.FromDateTime(end);

        // R-09: Kliniğin çalışma saatleri içinde olmalı.
        if (!AppointmentRules.FitsInWindow(startTime, endTime, service.OpeningTime, service.ClosingTime))
        {
            return ServiceResult<SlotContext>.Invalid(
                $"Klinik {service.OpeningTime:HH:mm} - {service.ClosingTime:HH:mm} saatleri arasında hizmet vermektedir. " +
                $"'{service.Name}' {service.Duration} dakika sürdüğü için {startTime:HH:mm} başlangıcı uygun değil.",
                nameof(dto.AppointmentDate));
        }

        // R-08: [başlangıç, bitiş] hekimin o günkü bir müsaitlik aralığının TAMAMEN içinde olmalı.
        var windows = await Repo<VeterinarianAvailability>().QueryNoTracking()
            .Where(a => a.VeterinarianId == dto.VeterinarianId && a.DayOfWeek == start.DayOfWeek)
            .OrderBy(a => a.StartTime)
            .Select(a => new { a.StartTime, a.EndTime })
            .ToListAsync(cancellationToken);

        if (!windows.Any(w => AppointmentRules.FitsInWindow(startTime, endTime, w.StartTime, w.EndTime)))
        {
            var dayName = DisplayText.Day(start.DayOfWeek);
            var hours = windows.Count == 0
                ? "bu gün çalışmıyor"
                : "çalışma saatleri: " + string.Join(", ", windows.Select(w => $"{w.StartTime:HH:mm}-{w.EndTime:HH:mm}"));
            return ServiceResult<SlotContext>.Invalid(
                $"{veterinarian.FullName} {dayName} günü {startTime:HH:mm}-{endTime:HH:mm} arasında müsait değil ({hours}).",
                nameof(dto.AppointmentDate));
        }

        // R-10: Hekimin o günkü aktif randevularıyla çakışmamalı (başlangıç VE bitiş karşılaştırılır).
        var dayStart = start.Date;
        var dayEnd = dayStart.AddDays(1);

        var vetAppointments = await Appointments.QueryNoTracking()
            .Where(a => a.VeterinarianId == dto.VeterinarianId
                && a.AppointmentDate >= dayStart && a.AppointmentDate < dayEnd
                && a.Status != AppointmentStatus.Rejected && a.Status != AppointmentStatus.Cancelled
                && (excludeAppointmentId == null || a.Id != excludeAppointmentId))
            .Select(a => new { a.AppointmentDate, a.Duration })
            .ToListAsync(cancellationToken);

        var vetConflict = vetAppointments.FirstOrDefault(a =>
            AppointmentRules.Overlaps(start, end, a.AppointmentDate, a.AppointmentDate.AddMinutes(a.Duration)));
        if (vetConflict is not null)
        {
            return ServiceResult<SlotContext>.Conflict(
                $"Seçilen saat dolu: {veterinarian.FullName} hekiminin {vetConflict.AppointmentDate:HH:mm}-" +
                $"{vetConflict.AppointmentDate.AddMinutes(vetConflict.Duration):HH:mm} arasında başka bir randevusu var. " +
                $"'{service.Name}' {service.Duration} dakika sürdüğü için {startTime:HH:mm}-{endTime:HH:mm} aralığı bu randevuyla çakışıyor.",
                nameof(dto.AppointmentDate));
        }

        // R-11: Aynı hayvanın aynı saatte başka bir aktif randevusu olmamalı.
        var petAppointments = await Appointments.QueryNoTracking()
            .Where(a => a.PetId == dto.PetId
                && a.AppointmentDate >= dayStart && a.AppointmentDate < dayEnd
                && a.Status != AppointmentStatus.Rejected && a.Status != AppointmentStatus.Cancelled
                && (excludeAppointmentId == null || a.Id != excludeAppointmentId))
            .Select(a => new { a.AppointmentDate, a.Duration })
            .ToListAsync(cancellationToken);

        var petConflict = petAppointments.FirstOrDefault(a =>
            AppointmentRules.Overlaps(start, end, a.AppointmentDate, a.AppointmentDate.AddMinutes(a.Duration)));
        if (petConflict is not null)
        {
            return ServiceResult<SlotContext>.Conflict(
                $"{pet.Name} adlı hayvanın {petConflict.AppointmentDate:HH:mm}-{petConflict.AppointmentDate.AddMinutes(petConflict.Duration):HH:mm} " +
                "arasında başka bir randevusu var.",
                nameof(dto.AppointmentDate));
        }

        return ServiceResult<SlotContext>.Success(new SlotContext(pet.OwnerId, start, service.Duration, service.Price));
    }

    // =====================================================================
    // ONAY AKIŞI
    // =====================================================================

    public async Task<ServiceResult> ApproveAsync(int id, CancellationToken cancellationToken = default)
    {
        var appointment = await FindForDecisionAsync(id, cancellationToken);
        if (appointment is null)
        {
            return ServiceResult.NotFound("Randevu bulunamadı.");
        }

        if (appointment.Status != AppointmentStatus.Pending)
        {
            return ServiceResult.Invalid($"Sadece 'Bekliyor' durumundaki randevular onaylanabilir. Mevcut durum: {DisplayText.Status(appointment.Status)}.");
        }

        if (appointment.AppointmentDate <= Now)
        {
            return ServiceResult.Invalid("Saati geçmiş bir randevu onaylanamaz; reddedebilir veya iptal edebilirsiniz.");
        }

        appointment.Status = AppointmentStatus.Approved;
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success("Randevu onaylandı.");
    }

    public async Task<ServiceResult> RejectAsync(int id, string reason, CancellationToken cancellationToken = default)
    {
        var appointment = await FindForDecisionAsync(id, cancellationToken);
        if (appointment is null)
        {
            return ServiceResult.NotFound("Randevu bulunamadı.");
        }

        if (appointment.Status != AppointmentStatus.Pending)
        {
            return ServiceResult.Invalid($"Sadece 'Bekliyor' durumundaki randevular reddedilebilir. Mevcut durum: {DisplayText.Status(appointment.Status)}.");
        }

        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length < 5)
        {
            return ServiceResult.Invalid("Ret gerekçesi en az 5 karakter olmalıdır.", "Reason");
        }

        appointment.Status = AppointmentStatus.Rejected;
        appointment.RejectionReason = reason.Length > 500 ? reason[..500] : reason;
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success("Randevu reddedildi. Saat tekrar boşa çıktı.");
    }

    public async Task<ServiceResult> CancelAsync(int id, CancellationToken cancellationToken = default)
    {
        var query = Appointments.Query().Where(a => a.Id == id);

        // Admin her randevuyu, üye sadece kendi randevusunu iptal edebilir. Veteriner iptal etmez (reddeder).
        if (!CurrentUser.IsAdmin)
        {
            if (!CurrentUser.IsMember || CurrentUser.UserId is not string userId)
            {
                return ServiceResult.Forbidden("Randevuyu sadece sahibi veya admin iptal edebilir.");
            }

            query = query.Where(a => a.OwnerId == userId);
        }

        var appointment = await query.FirstOrDefaultAsync(cancellationToken);
        if (appointment is null)
        {
            return ServiceResult.NotFound("Randevu bulunamadı.");
        }

        if (appointment.Status is not (AppointmentStatus.Pending or AppointmentStatus.Approved))
        {
            return ServiceResult.Invalid($"Bu randevu iptal edilemez. Mevcut durum: {DisplayText.Status(appointment.Status)}.");
        }

        if (!CurrentUser.IsAdmin && appointment.AppointmentDate <= Now)
        {
            return ServiceResult.Invalid("Saati geçmiş bir randevu iptal edilemez.");
        }

        appointment.Status = AppointmentStatus.Cancelled;
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success("Randevu iptal edildi.");
    }

    public async Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!CurrentUser.IsAdmin)
        {
            return ServiceResult.Forbidden();
        }

        var appointment = await Appointments.Query()
            .Include(a => a.ExaminationNote)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (appointment is null)
        {
            return ServiceResult.NotFound("Randevu bulunamadı.");
        }

        // Geçmiş sağlık kayıtları kaybolmasın: sadece iptal/ret edilmiş ve notu olmayan randevular silinir.
        if (appointment.Status is not (AppointmentStatus.Cancelled or AppointmentStatus.Rejected) || appointment.ExaminationNote is not null)
        {
            return ServiceResult.Invalid("Sadece iptal edilmiş veya reddedilmiş (muayene notu olmayan) randevular silinebilir.");
        }

        Appointments.Remove(appointment);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success("Randevu kaydı silindi.");
    }

    // =====================================================================
    // YARDIMCI METOTLAR
    // =====================================================================

    /// <summary>Onay/ret yetkisi: Admin veya randevunun hekimi.</summary>
    private async Task<Appointment?> FindForDecisionAsync(int id, CancellationToken cancellationToken)
    {
        var query = Appointments.Query().Where(a => a.Id == id);

        if (!CurrentUser.IsAdmin)
        {
            if (!CurrentUser.IsVeterinarian || CurrentUser.VeterinarianId is not int veterinarianId)
            {
                return null;
            }

            query = query.Where(a => a.VeterinarianId == veterinarianId);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>Düzenleme yetkisi: Admin veya sahibi; sadece Bekliyor durumundaki ileri tarihli randevular.</summary>
    private async Task<Appointment?> FindEditableAsync(int id, CancellationToken cancellationToken)
    {
        var now = Now;
        var query = Appointments.Query()
            .Where(a => a.Id == id && a.Status == AppointmentStatus.Pending && a.AppointmentDate > now);

        if (!CurrentUser.IsAdmin)
        {
            if (!CurrentUser.IsMember || CurrentUser.UserId is not string userId)
            {
                return null;
            }

            query = query.Where(a => a.OwnerId == userId);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<AppointmentDto> LoadDtoAsync(int id, CancellationToken cancellationToken)
    {
        return await Appointments.QueryNoTracking()
            .Where(a => a.Id == id)
            .Select(Projections.Appointment)
            .FirstAsync(cancellationToken);
    }

    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    /// <summary>Kurallardan geçen randevunun kaydedilecek değerleri.</summary>
    private sealed record SlotContext(string OwnerId, DateTime Start, int Duration, decimal Price);
}
