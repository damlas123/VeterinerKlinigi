using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Web.Infrastructure;
using VeterinerKlinigi.Web.Models;

namespace VeterinerKlinigi.Web.Controllers.Shared;

/// <summary>
/// Aşı kaydı ekleme/düzenleme/silme: Admin ve Veteriner panelinde ortak.
/// Yetki kontrolü (veteriner sadece hastasına aşı girebilir) servis katmanındadır.
/// Görünüm: Views/Shared/VaccinationForm.cshtml
/// </summary>
public abstract class VaccinationsControllerBase : AppController
{
    protected VaccinationsControllerBase(IVaccinationService vaccinationService, IPetService petService)
    {
        VaccinationService = vaccinationService;
        PetService = petService;
    }

    protected IVaccinationService VaccinationService { get; }

    protected IPetService PetService { get; }

    /// <summary>İşlem sonrası hayvanın sağlık geçmişi sayfasına dönülür.</summary>
    protected abstract IActionResult BackToPet(int petId);

    protected abstract string PetUrl(int petId);

    protected async Task<IActionResult> FormViewAsync(VaccinationSaveDto model, int? id, CancellationToken cancellationToken)
    {
        var pet = await PetService.GetDetailAsync(model.PetId, cancellationToken);
        if (!pet.IsSuccess)
        {
            return HandleFailure(pet);
        }

        ViewData["Title"] = id is null ? "Aşı kaydı ekle" : "Aşı kaydını düzenle";
        ViewData["Id"] = id;
        ViewData["PetName"] = pet.Data!.Pet.Name;
        ViewData["CancelUrl"] = PetUrl(model.PetId);
        return View("VaccinationForm", model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int petId, CancellationToken cancellationToken)
    {
        var model = new VaccinationSaveDto { PetId = petId, VaccinationDate = DateOnly.FromDateTime(DateTime.Today) };
        return await FormViewAsync(model, null, cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Create(VaccinationSaveDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await FormViewAsync(model, null, cancellationToken);
        }

        var result = await VaccinationService.CreateAsync(model, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorType == ErrorType.Forbidden)
            {
                return Forbid();
            }

            ModelState.AddResultError(result);
            return await FormViewAsync(model, null, cancellationToken);
        }

        ShowSuccess(result.Message);
        return BackToPet(model.PetId);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await VaccinationService.GetForEditAsync(id, cancellationToken);
        return result.IsSuccess ? await FormViewAsync(result.Data!, id, cancellationToken) : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, VaccinationSaveDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await FormViewAsync(model, id, cancellationToken);
        }

        var result = await VaccinationService.UpdateAsync(id, model, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorType is ErrorType.NotFound or ErrorType.Forbidden)
            {
                return HandleFailure(result);
            }

            ModelState.AddResultError(result);
            return await FormViewAsync(model, id, cancellationToken);
        }

        ShowSuccess(result.Message);
        return BackToPet(model.PetId);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await VaccinationService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        var v = result.Data!;
        return View("DeleteConfirm", new DeleteConfirmViewModel
        {
            Id = id.ToString(),
            Title = "Aşı kaydını sil",
            Question = "Bu aşı kaydını silmek istediğinize emin misiniz? Bu işlem geri alınamaz.",
            Details =
            [
                new("Evcil hayvan", v.PetName),
                new("Aşı", v.VaccineName),
                new("Uygulama tarihi", v.VaccinationDate.ToString("dd.MM.yyyy")),
                new("Sonraki doz", v.NextDoseDate?.ToString("dd.MM.yyyy") ?? "-")
            ],
            CancelUrl = PetUrl(v.PetId)
        });
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await VaccinationService.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        ShowSuccess(result.Message);
        return BackToPet(result.Data);
    }
}
