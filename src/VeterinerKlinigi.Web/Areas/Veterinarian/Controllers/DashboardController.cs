using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;

namespace VeterinerKlinigi.Web.Areas.Veterinarian.Controllers;

public class DashboardController : VeterinarianAreaController
{
    private readonly IReportService _reportService;

    public DashboardController(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _reportService.GetVeterinarianDashboardAsync(cancellationToken);
        return result.IsSuccess ? View(result.Data) : HandleFailure(result);
    }
}
