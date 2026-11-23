using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Abstractions;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Entities.Constants;
using VeterinerKlinigi.Web.Controllers.Shared;

namespace VeterinerKlinigi.Web.Areas.Veterinarian.Controllers;

/// <summary>Veteriner: kendi müsaitlik saatleri (ortak kod: AvailabilityControllerBase).</summary>
[Area("Veterinarian")]
[Authorize(Roles = RoleNames.Veterinarian)]
public class AvailabilityController : AvailabilityControllerBase
{
    private readonly ICurrentUserService _currentUser;

    public AvailabilityController(IAvailabilityService availabilityService, ICurrentUserService currentUser)
        : base(availabilityService)
    {
        _currentUser = currentUser;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (_currentUser.VeterinarianId is not int veterinarianId)
        {
            return Forbid();
        }

        return View(await AvailabilityService.GetByVeterinarianAsync(veterinarianId, cancellationToken));
    }

    [HttpGet]
    public IActionResult Create()
        => _currentUser.VeterinarianId is int veterinarianId
            ? FormView(new AvailabilitySaveDto { VeterinarianId = veterinarianId }, null)
            : Forbid();

    protected override IActionResult BackToList(int veterinarianId) => RedirectToAction(nameof(Index));

    protected override string CancelUrl(int veterinarianId) => Url.Action(nameof(Index))!;
}
