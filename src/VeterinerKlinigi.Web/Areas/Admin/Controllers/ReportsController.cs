using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VeterinerKlinigi.Business.Abstract;

namespace VeterinerKlinigi.Web.Areas.Admin.Controllers;

/// <summary>
/// Raporlar sayfası. Tablo ve grafik verileri sayfadaki jQuery koduyla
/// REST API'den (GET /api/reports/revenue) alınır.
/// </summary>
public class ReportsController : AdminController
{
    private readonly IClinicService _clinicService;

    public ReportsController(IClinicService clinicService)
    {
        _clinicService = clinicService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        ViewData["From"] = new DateOnly(today.Year, today.Month, 1).AddMonths(-5).ToString("yyyy-MM-dd");
        ViewData["To"] = today.ToString("yyyy-MM-dd");
        ViewData["Clinics"] = (await _clinicService.GetAllAsync(includeInactive: true, cancellationToken))
            .Select(c => new SelectListItem(c.Name, c.Id.ToString()))
            .ToList();
        return View();
    }
}
