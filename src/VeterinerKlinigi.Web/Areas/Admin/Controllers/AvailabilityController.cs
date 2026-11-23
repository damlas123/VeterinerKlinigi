using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Entities.Constants;
using VeterinerKlinigi.Web.Controllers.Shared;

namespace VeterinerKlinigi.Web.Areas.Admin.Controllers;

/// <summary>Admin: herhangi bir hekimin müsaitlik saatlerini yönetir (ortak kod: AvailabilityControllerBase).</summary>
[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public class AvailabilityController : AvailabilityControllerBase
{
    public AvailabilityController(IAvailabilityService availabilityService) : base(availabilityService)
    {
    }

    // GET: /Admin/Availability/Create?veterinarianId=3
    [HttpGet]
    public IActionResult Create(int veterinarianId)
        => FormView(new AvailabilitySaveDto { VeterinarianId = veterinarianId }, null);

    protected override IActionResult BackToList(int veterinarianId)
        => RedirectToAction("Details", "Veterinarians", new { id = veterinarianId });

    protected override string CancelUrl(int veterinarianId)
        => Url.Action("Details", "Veterinarians", new { id = veterinarianId })!;
}
