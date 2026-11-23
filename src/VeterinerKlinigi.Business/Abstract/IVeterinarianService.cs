using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.Business.Abstract;

/// <summary>Veteriner hekim işlemleri.</summary>
public interface IVeterinarianService
{
    /// <summary>Hekimleri listeler; klinik, hizmet ve uzmanlık alanına göre LINQ ile filtreler.</summary>
    Task<List<VeterinarianDto>> GetAllAsync(VeterinarianFilter? filter = null, CancellationToken cancellationToken = default);

    Task<ServiceResult<VeterinarianDetailDto>> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<VeterinarianUpdateDto>> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Sistemdeki farklı uzmanlık alanları (filtre listesi için).</summary>
    Task<List<string>> GetSpecialtiesAsync(CancellationToken cancellationToken = default);

    /// <summary>Hem giriş hesabını (Veteriner rolüyle) hem de hekim kaydını oluşturur.</summary>
    Task<ServiceResult<int>> CreateAsync(VeterinarianCreateDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(int id, VeterinarianUpdateDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult<DeleteOutcome>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
