using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.Web.Areas.Veterinarian.Controllers;

/// <summary>
/// Veteriner: hastaları (kendisiyle randevusu olan hayvanlar) ve sağlık geçmişleri.
/// Randevusu olmayan bir hayvanın id'si yazılırsa servis "bulunamadı" döner.
/// </summary>
public class PatientsController : VeterinarianAreaController
{
    private readonly IPetService _petService;

    public PatientsController(IPetService petService)
    {
        _petService = petService;
    }

    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        ViewData["Search"] = search;
        return View(await _petService.GetListAsync(new PetFilter { Search = search }, cancellationToken));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var result = await _petService.GetDetailAsync(id, cancellationToken);
        return result.IsSuccess ? View("PetDetails", result.Data) : HandleFailure(result);
    }
}
