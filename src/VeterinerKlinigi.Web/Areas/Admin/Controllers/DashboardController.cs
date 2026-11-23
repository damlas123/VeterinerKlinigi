using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;

namespace VeterinerKlinigi.Web.Areas.Admin.Controllers;

public class DashboardController : AdminController
{
    private readonly IReportService _reportService;

    public DashboardController(IReportService reportService)
    {
        _reportService = reportService;
    }

    // GET: /Admin — istatistik kartları sunucuda, grafikler /api/reports/dashboard ucundan (jQuery) yüklenir.
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _reportService.GetAdminDashboardAsync(cancellationToken);
        return result.IsSuccess ? View(result.Data) : HandleFailure(result);
    }
}
