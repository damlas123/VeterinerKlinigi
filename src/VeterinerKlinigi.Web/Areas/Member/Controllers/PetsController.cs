using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Web.Infrastructure;
using VeterinerKlinigi.Web.Models;

namespace VeterinerKlinigi.Web.Areas.Member.Controllers;

/// <summary>
/// Üye: evcil hayvan CRUD + güvenli fotoğraf yükleme.
/// Başka bir üyenin hayvan id'si yazılırsa servis "bulunamadı" döner → 404.
/// </summary>
public class PetsController : MemberAreaController
{
    private readonly IPetService _petService;
    private readonly IPetPhotoStorage _photoStorage;

    public PetsController(IPetService petService, IPetPhotoStorage photoStorage)
    {
        _petService = petService;
        _photoStorage = photoStorage;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _petService.GetListAsync(null, cancellationToken));

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var result = await _petService.GetDetailAsync(id, cancellationToken);
        return result.IsSuccess ? View("PetDetails", result.Data) : HandleFailure(result);
    }

    [HttpGet]
    public IActionResult Create() => FormView(new PetFormViewModel { BirthDate = DateOnly.FromDateTime(DateTime.Today).AddYears(-1) });

    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)] // İstek boyutu sınırı (fotoğraf en fazla 2 MB, form alanlarıyla birlikte 5 MB)
    public async Task<IActionResult> Create(PetFormViewModel model, CancellationToken cancellationToken)
    {
        ValidatePhoto(model);
        if (!ModelState.IsValid)
        {
            return FormView(model);
        }

        string? photoUrl = null;
        if (model.Photo is not null)
        {
            photoUrl = await _photoStorage.SaveAsync(model.Photo, cancellationToken);
        }

        var result = await _petService.CreateAsync(model, photoUrl, cancellationToken);
        if (!result.IsSuccess)
        {
            _photoStorage.Delete(photoUrl); // Kayıt başarısızsa yüklenen dosya da silinir.
            ModelState.AddResultError(result);
            return FormView(model);
        }

        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Details), new { id = result.Data });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await _petService.GetForEditAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        var form = result.Data!.Form;
        return FormView(new PetFormViewModel
        {
            Id = id,
            Name = form.Name,
            Species = form.Species,
            Breed = form.Breed,
            BirthDate = form.BirthDate,
            Weight = form.Weight,
            CurrentPhotoUrl = result.Data.PhotoUrl
        });
    }

    [HttpPost]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> Edit(int id, PetFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        ValidatePhoto(model);
        if (!ModelState.IsValid)
        {
            return FormView(model);
        }

        string? newPhotoUrl = null;
        if (model.Photo is not null)
        {
            newPhotoUrl = await _photoStorage.SaveAsync(model.Photo, cancellationToken);
        }

        var result = await _petService.UpdateAsync(id, model, newPhotoUrl, model.RemovePhoto, cancellationToken);
        if (!result.IsSuccess)
        {
            _photoStorage.Delete(newPhotoUrl);
            if (result.ErrorType == ErrorType.NotFound)
            {
                return NotFound();
            }

            ModelState.AddResultError(result);
            return FormView(model);
        }

        _photoStorage.Delete(result.Data); // Değiştirilen / kaldırılan eski fotoğraf diskten silinir.
        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _petService.GetForEditAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        var pet = result.Data!.Form;
        return View("DeleteConfirm", new DeleteConfirmViewModel
        {
            Id = id.ToString(),
            Title = "Evcil hayvanı sil",
            Question = $"{pet.Name} kaydını silmek istediğinize emin misiniz?",
            Details = [new("Tür", DisplayText.Species(pet.Species)), new("Irk", pet.Breed ?? "-"), new("Doğum tarihi", pet.BirthDate.ToString("dd.MM.yyyy"))],
            Note = "Aşı veya randevu geçmişi varsa kayıt silinmez, arşivlenir (sağlık geçmişi korunur) ve ileri tarihli randevuları iptal edilir.",
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

    private void ValidatePhoto(PetFormViewModel model)
    {
        if (model.Photo is not null && _photoStorage.Validate(model.Photo) is string error)
        {
            ModelState.AddModelError(nameof(model.Photo), error);
        }
    }

    private ViewResult FormView(PetFormViewModel model)
    {
        ViewData["Title"] = model.Id is null ? "Evcil hayvan ekle" : $"{model.Name} - düzenle";
        return View("Form", model);
    }
}
