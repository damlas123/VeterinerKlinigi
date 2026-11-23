using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;

namespace VeterinerKlinigi.Web.Areas.Member.Controllers;

/// <summary>Üye: evcil hayvanlarının aşı takvimi (sadece görüntüleme).</summary>
public class VaccinationsController : MemberAreaController
{
    private readonly IVaccinationService _vaccinationService;

    public VaccinationsController(IVaccinationService vaccinationService)
    {
        _vaccinationService = vaccinationService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _vaccinationService.GetSchedulesAsync(cancellationToken));
}
