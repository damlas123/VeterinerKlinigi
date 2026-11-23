using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.Business.Abstract;

/// <summary>Muayene notları. Not girilmesi randevuyu "Tamamlandı" durumuna getirir.</summary>
public interface IExaminationNoteService
{
    /// <summary>Veteriner: kendi yazdığı notlar, Admin: tümü.</summary>
    Task<List<ExaminationNoteDto>> GetListAsync(CancellationToken cancellationToken = default);

    Task<ServiceResult<ExaminationNoteDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Onaylanmış randevuya muayene notu ekler ve randevuyu tamamlar.</summary>
    Task<ServiceResult<int>> CompleteAppointmentAsync(int appointmentId, ExaminationNoteSaveDto dto, CancellationToken cancellationToken = default);

    /// <summary>Notu sadece yazan hekim düzenleyebilir.</summary>
    Task<ServiceResult> UpdateAsync(int id, ExaminationNoteSaveDto dto, CancellationToken cancellationToken = default);
}
