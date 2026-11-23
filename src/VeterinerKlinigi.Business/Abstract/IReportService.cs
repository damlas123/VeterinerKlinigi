using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.Business.Abstract;

/// <summary>Raporlar ve panel (dashboard) istatistikleri.</summary>
public interface IReportService
{
    /// <summary>Hizmet türüne göre randevu sayısı ve ciro (LINQ GroupBy).</summary>
    Task<ServiceResult<RevenueReportDto>> GetRevenueByServiceAsync(DateOnly? from, DateOnly? to, int? clinicId, CancellationToken cancellationToken = default);

    Task<ServiceResult<AdminDashboardDto>> GetAdminDashboardAsync(CancellationToken cancellationToken = default);

    Task<ServiceResult<VeterinarianDashboardDto>> GetVeterinarianDashboardAsync(CancellationToken cancellationToken = default);

    Task<ServiceResult<MemberDashboardDto>> GetMemberDashboardAsync(CancellationToken cancellationToken = default);
}
