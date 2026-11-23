using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.API.Auth;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.API.Controllers;

/// <summary>
/// Raporlar (sadece Admin). Admin panelindeki grafikler de verilerini bu uçlardan (jQuery ile) alır.
/// </summary>
[Route("api/reports")]
[Authorize(Policy = ApiPolicies.AdminRead)]
public sealed class ReportsController : ApiControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>
    /// GET /api/reports/revenue?from=2026-06-01&amp;to=2026-11-30&amp;clinicId=1
    /// Hizmet türüne göre randevu sayısı ve ciro (LINQ GroupBy).
    /// </summary>
    [HttpGet("revenue")]
    [ProducesResponseType<RevenueReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RevenueReportDto>> GetRevenue(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? clinicId,
        CancellationToken cancellationToken)
        => FromResult(await _reportService.GetRevenueByServiceAsync(from, to, clinicId, cancellationToken));

    /// <summary>GET /api/reports/dashboard — admin paneli istatistikleri ve grafik verileri.</summary>
    [HttpGet("dashboard")]
    [ProducesResponseType<AdminDashboardDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AdminDashboardDto>> GetDashboard(CancellationToken cancellationToken)
        => FromResult(await _reportService.GetAdminDashboardAsync(cancellationToken));
}
