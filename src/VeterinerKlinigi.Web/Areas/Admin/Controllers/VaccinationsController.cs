using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Entities.Constants;
using VeterinerKlinigi.Web.Controllers.Shared;

namespace VeterinerKlinigi.Web.Areas.Admin.Controllers;

/// <summary>Admin: aşı kayıtları (ortak kod: VaccinationsControllerBase).</summary>
[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class VaccinationsController : VaccinationsControllerBase
{
    public VaccinationsController(IVaccinationService vaccinationService, IPetService petService)
        : base(vaccinationService, petService)
    {
    }

    protected override IActionResult BackToPet(int petId) => RedirectToAction("Details", "Pets", new { id = petId });

    protected override string PetUrl(int petId) => Url.Action("Details", "Pets", new { id = petId })!;
}
