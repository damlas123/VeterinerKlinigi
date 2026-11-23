using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Web.Infrastructure;
using VeterinerKlinigi.Web.Models;

namespace VeterinerKlinigi.Web.Areas.Admin.Controllers;

/// <summary>Admin: tüm evcil hayvanlar, sağlık geçmişi ve silme/arşivleme.</summary>
public class PetsController : AdminController
{
    private readonly IPetService _petService;
    private readonly IPetPhotoStorage _photoStorage;

    public PetsController(IPetService petService, IPetPhotoStorage photoStorage)
    {
        _petService = petService;
        _photoStorage = photoStorage;
    }

    public async Task<IActionResult> Index(string? search, bool includeInactive, CancellationToken cancellationToken)
    {
        ViewData["Search"] = search;
        ViewData["IncludeInactive"] = includeInactive;
        return View(await _petService.GetListAsync(new PetFilter { Search = search, IncludeInactive = includeInactive }, cancellationToken));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var result = await _petService.GetDetailAsync(id, cancellationToken);
        return result.IsSuccess ? View("PetDetails", result.Data) : HandleFailure(result);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _petService.GetDetailAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        var pet = result.Data!.Pet;
        return View("DeleteConfirm", new DeleteConfirmViewModel
        {
            Id = id.ToString(),
            Title = "Evcil hayvanı sil",
            Question = $"{pet.Name} kaydını silmek istediğinize emin misiniz?",
            Details = [new("Sahibi", pet.OwnerName), new("Tür", pet.SpeciesName), new("Irk", pet.Breed ?? "-")],
            Note = "Aşı veya randevu geçmişi varsa kayıt silinmez, arşivlenir; ileri tarihli randevuları iptal edilir.",
            CancelUrl = Url.Action(nameof(Details), new { id })!
        });
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await _petService.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        if (result.Data!.Outcome == DeleteOutcome.Deleted)
        {
            _photoStorage.Delete(result.Data.PhotoUrl);
        }

        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }
}
