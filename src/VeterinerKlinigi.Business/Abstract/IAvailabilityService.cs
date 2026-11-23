using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.Business.Abstract;

/// <summary>Hekim müsaitlik (çalışma) saatleri. Admin tümünü, veteriner sadece kendisininkini yönetir.</summary>
public interface IAvailabilityService
{
    Task<List<AvailabilityDto>> GetByVeterinarianAsync(int veterinarianId, CancellationToken cancellationToken = default);

    Task<ServiceResult<AvailabilitySaveDto>> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<int>> CreateAsync(AvailabilitySaveDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(int id, AvailabilitySaveDto dto, CancellationToken cancellationToken = default);

    /// <summary>Silinen kaydın hekim Id'sini döndürür (listeye geri dönmek için).</summary>
    Task<ServiceResult<int>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
