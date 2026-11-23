using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Entities.Constants;
using VeterinerKlinigi.Web.Controllers.Shared;

namespace VeterinerKlinigi.Web.Areas.Veterinarian.Controllers;

/// <summary>Veteriner: hastalarının aşı kayıtları (ortak kod: VaccinationsControllerBase).</summary>
[Area("Veterinarian")]
[Authorize(Roles = RoleNames.Veterinarian)]
public class VaccinationsController : VaccinationsControllerBase
{
    public VaccinationsController(IVaccinationService vaccinationService, IPetService petService)
        : base(vaccinationService, petService)
    {
    }

    // GET: /Veterinarian/Vaccinations — hastaların aşı takvimleri
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await VaccinationService.GetSchedulesAsync(cancellationToken));

    protected override IActionResult BackToPet(int petId) => RedirectToAction("Details", "Patients", new { id = petId });

    protected override string PetUrl(int petId) => Url.Action("Details", "Patients", new { id = petId })!;
}
