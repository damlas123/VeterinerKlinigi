using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;

namespace VeterinerKlinigi.Web.Areas.Member.Controllers;

public class DashboardController : MemberAreaController
{
    private readonly IReportService _reportService;

    public DashboardController(IReportService reportService)
    {
        _reportService = reportService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var result = await _reportService.GetMemberDashboardAsync(cancellationToken);
        return result.IsSuccess ? View(result.Data) : HandleFailure(result);
    }
}
