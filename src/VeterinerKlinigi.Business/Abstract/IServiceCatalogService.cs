using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.Business.Abstract;

/// <summary>
/// Hizmet (muayene, aşı, kısırlaştırma ...) işlemleri.
/// "IServiceService" gibi kafa karıştırıcı bir ad olmasın diye "hizmet kataloğu" adını kullandık.
/// </summary>
public interface IServiceCatalogService
{
    Task<List<ServiceDto>> GetAllAsync(int? clinicId = null, bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<ServiceResult<ServiceDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<ServiceSaveDto>> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<int>> CreateAsync(ServiceSaveDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(int id, ServiceSaveDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult<DeleteOutcome>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
