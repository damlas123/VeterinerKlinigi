using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.Business.Abstract;

/// <summary>Klinik işlemleri (CRUD: Admin).</summary>
public interface IClinicService
{
    Task<List<ClinicDto>> GetAllAsync(bool includeInactive = false, CancellationToken cancellationToken = default);

    Task<ServiceResult<ClinicDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<ClinicSaveDto>> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<int>> CreateAsync(ClinicSaveDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(int id, ClinicSaveDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult<DeleteOutcome>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
