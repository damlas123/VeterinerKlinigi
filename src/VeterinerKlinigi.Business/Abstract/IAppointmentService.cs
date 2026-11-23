using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.Business.Abstract;

/// <summary>
/// Randevu işlemleri. Projenin en önemli iş kuralları (çakışma kontrolü, uygun saat hesaplama,
/// onay akışı) bu servistedir ve MVC, REST API ile MCP tarafından ORTAK kullanılır.
/// </summary>
public interface IAppointmentService
{
    Task<List<AppointmentDto>> GetListAsync(AppointmentFilter? filter = null, CancellationToken cancellationToken = default);

    Task<ServiceResult<AppointmentDetailDto>> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<AppointmentSaveDto>> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Seçilen hizmet ve tarih için uygun hekimleri ve boş saatleri döndürür.
    /// Hekimin müsaitliği, kliniğin çalışma saatleri, hizmet süresi ve mevcut randevular dikkate alınır.
    /// </summary>
    Task<ServiceResult<List<VeterinarianSlotsDto>>> GetAvailableSlotsAsync(
        int serviceId, DateOnly date, int? veterinarianId = null, int? excludeAppointmentId = null,
        CancellationToken cancellationToken = default);

    /// <summary>Randevu oluşturur (durum: Bekliyor). Tüm kurallar burada kontrol edilir.</summary>
    Task<ServiceResult<AppointmentDto>> CreateAsync(AppointmentSaveDto dto, CancellationToken cancellationToken = default);

    /// <summary>Bekleyen randevuyu günceller (tarih/saat/hekim/hizmet). Kurallar yeniden kontrol edilir.</summary>
    Task<ServiceResult<AppointmentDto>> UpdateAsync(int id, AppointmentSaveDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> ApproveAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult> RejectAsync(int id, string reason, CancellationToken cancellationToken = default);

    Task<ServiceResult> CancelAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Admin: sadece iptal edilmiş veya reddedilmiş (muayene notu olmayan) randevular silinebilir.</summary>
    Task<ServiceResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
