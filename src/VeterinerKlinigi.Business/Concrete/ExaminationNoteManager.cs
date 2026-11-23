using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Data.Repositories;
using VeterinerKlinigi.Entities;
using VeterinerKlinigi.Entities.Enums;

namespace VeterinerKlinigi.Business.Concrete;

public class ExaminationNoteManager : ManagerBase, IExaminationNoteService
{
    public ExaminationNoteManager(IUnitOfWork unitOfWork, ICurrentUserService currentUser, TimeProvider timeProvider)
        : base(unitOfWork, currentUser, timeProvider)
    {
    }

    private IRepository<ExaminationNote> Notes => Repo<ExaminationNote>();

    public async Task<List<ExaminationNoteDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        var query = Notes.QueryNoTracking();

        if (!CurrentUser.IsAdmin)
        {
            if (CurrentUser.VeterinarianId is not int veterinarianId)
            {
                return [];
            }

            query = query.Where(n => n.VeterinarianId == veterinarianId);
        }

        return await query
            .OrderByDescending(n => n.Appointment.AppointmentDate)
            .Select(Projections.ExaminationNote)
            .ToListAsync(cancellationToken);
    }

    public async Task<ServiceResult<ExaminationNoteDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var query = Notes.QueryNoTracking().Where(n => n.Id == id);

        if (!CurrentUser.IsAdmin)
        {
            if (CurrentUser.VeterinarianId is not int veterinarianId)
            {
                return ServiceResult<ExaminationNoteDto>.NotFound("Muayene notu bulunamadı.");
            }

            query = query.Where(n => n.VeterinarianId == veterinarianId);
        }

        var note = await query.Select(Projections.ExaminationNote).FirstOrDefaultAsync(cancellationToken);
        return note is null
            ? ServiceResult<ExaminationNoteDto>.NotFound("Muayene notu bulunamadı.")
            : ServiceResult<ExaminationNoteDto>.Success(note);
    }

    public async Task<ServiceResult<int>> CompleteAppointmentAsync(int appointmentId, ExaminationNoteSaveDto dto, CancellationToken cancellationToken = default)
    {
        // Muayene notunu sadece randevunun hekimi girebilir.
        if (!CurrentUser.IsVeterinarian || CurrentUser.VeterinarianId is not int veterinarianId)
        {
            return ServiceResult<int>.Forbidden("Muayene notunu sadece randevunun veteriner hekimi girebilir.");
        }

        var appointment = await Repo<Appointment>().Query()
            .Include(a => a.ExaminationNote)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && a.VeterinarianId == veterinarianId, cancellationToken);
        if (appointment is null)
        {
            return ServiceResult<int>.NotFound("Randevu bulunamadı.");
        }

        if (appointment.Status != AppointmentStatus.Approved)
        {
            return ServiceResult<int>.Invalid($"Sadece onaylanmış randevular tamamlanabilir. Mevcut durum: {DisplayText.Status(appointment.Status)}.");
        }

        if (appointment.AppointmentDate > Now)
        {
            return ServiceResult<int>.Invalid("Randevu saati gelmeden muayene notu girilemez.");
        }

        if (appointment.ExaminationNote is not null)
        {
            return ServiceResult<int>.Conflict("Bu randevunun muayene notu zaten girilmiş.");
        }

        var note = new ExaminationNote
        {
            AppointmentId = appointmentId,
            VeterinarianId = veterinarianId,
            CreatedAt = Now
        };
        Map(dto, note);

        // Not eklenince randevu "Tamamlandı" olur; ikisi aynı SaveChanges ile (tek işlemde) kaydedilir.
        appointment.Status = AppointmentStatus.Completed;
        await Notes.AddAsync(note, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);

        return ServiceResult<int>.Success(note.Id, "Muayene notu kaydedildi, randevu tamamlandı.");
    }

    public async Task<ServiceResult> UpdateAsync(int id, ExaminationNoteSaveDto dto, CancellationToken cancellationToken = default)
    {
        if (CurrentUser.VeterinarianId is not int veterinarianId)
        {
            return ServiceResult.Forbidden("Muayene notunu sadece yazan hekim düzenleyebilir.");
        }

        var note = await Notes.Query().FirstOrDefaultAsync(n => n.Id == id && n.VeterinarianId == veterinarianId, cancellationToken);
        if (note is null)
        {
            return ServiceResult.NotFound("Muayene notu bulunamadı.");
        }

        Map(dto, note);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return ServiceResult.Success("Muayene notu güncellendi.");
    }

    private static void Map(ExaminationNoteSaveDto dto, ExaminationNote note)
    {
        note.Diagnosis = dto.Diagnosis.Trim();
        note.Treatment = dto.Treatment.Trim();
        note.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
    }
}
