using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.Business.Abstract;

/// <summary>Aşı kayıtları ve aşı takvimi.</summary>
public interface IVaccinationService
{
    /// <summary>Kullanıcının görebildiği tüm aktif hayvanların aşı takvimleri.</summary>
    Task<List<VaccinationScheduleDto>> GetSchedulesAsync(CancellationToken cancellationToken = default);

    /// <summary>Tek bir hayvanın aşı geçmişi ve yaklaşan aşıları (GET /api/pets/{id}/vaccinations).</summary>
    Task<ServiceResult<VaccinationScheduleDto>> GetScheduleByPetAsync(int petId, CancellationToken cancellationToken = default);

    /// <summary>Hayvan adına göre aşı takvimi (MCP: asi_takvimi). Sadece kullanıcının kendi hayvanlarında arar.</summary>
    Task<ServiceResult<List<VaccinationScheduleDto>>> GetScheduleByPetNameAsync(string petName, CancellationToken cancellationToken = default);

    /// <summary>Önümüzdeki <paramref name="days"/> gün içindeki ve gecikmiş aşılar.</summary>
    Task<List<VaccinationDto>> GetDueAsync(int days = 30, CancellationToken cancellationToken = default);

    Task<ServiceResult<VaccinationDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<VaccinationSaveDto>> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<int>> CreateAsync(VaccinationSaveDto dto, CancellationToken cancellationToken = default);

    Task<ServiceResult> UpdateAsync(int id, VaccinationSaveDto dto, CancellationToken cancellationToken = default);

    /// <summary>Silinen kaydın hayvan Id'sini döndürür.</summary>
    Task<ServiceResult<int>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
