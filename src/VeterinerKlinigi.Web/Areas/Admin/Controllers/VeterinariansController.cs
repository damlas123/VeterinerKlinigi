using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Web.Infrastructure;
using VeterinerKlinigi.Web.Models;

namespace VeterinerKlinigi.Web.Areas.Admin.Controllers;

/// <summary>Veteriner hekim yönetimi: giriş hesabı + hekim kaydı + verdiği hizmetler + çalışma saatleri.</summary>
public class VeterinariansController : AdminController
{
    private readonly IVeterinarianService _veterinarianService;
    private readonly IClinicService _clinicService;
    private readonly IServiceCatalogService _serviceCatalog;

    public VeterinariansController(IVeterinarianService veterinarianService, IClinicService clinicService, IServiceCatalogService serviceCatalog)
    {
        _veterinarianService = veterinarianService;
        _clinicService = clinicService;
        _serviceCatalog = serviceCatalog;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _veterinarianService.GetAllAsync(new VeterinarianFilter { IncludeInactive = true }, cancellationToken));

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var result = await _veterinarianService.GetDetailAsync(id, cancellationToken);
        return result.IsSuccess ? View(result.Data) : HandleFailure(result);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        await LoadOptionsAsync(null, cancellationToken);
        return View(new VeterinarianCreateDto());
    }

    [HttpPost]
    public async Task<IActionResult> Create(VeterinarianCreateDto model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _veterinarianService.CreateAsync(model, cancellationToken);
            if (result.IsSuccess)
            {
                ShowSuccess(result.Message);
                return RedirectToAction(nameof(Details), new { id = result.Data });
            }

            ModelState.AddResultError(result);
        }

        await LoadOptionsAsync(model.ServiceIds, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await _veterinarianService.GetForEditAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        ViewData["Id"] = id;
        await LoadOptionsAsync(result.Data!.ServiceIds, cancellationToken);
        return View(result.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, VeterinarianUpdateDto model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _veterinarianService.UpdateAsync(id, model, cancellationToken);
            if (result.IsSuccess)
            {
                ShowSuccess(result.Message);
                return RedirectToAction(nameof(Details), new { id });
            }

            if (result.ErrorType == ErrorType.NotFound)
            {
                return NotFound();
            }

            ModelState.AddResultError(result);
        }

        ViewData["Id"] = id;
        await LoadOptionsAsync(model.ServiceIds, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _veterinarianService.GetDetailAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        var vet = result.Data!.Veterinarian;
        return View("DeleteConfirm", new DeleteConfirmViewModel
        {
            Id = id.ToString(),
            Title = "Veterineri sil",
            Question = $"{vet.FullName} kaydını silmek istediğinize emin misiniz?",
            Details =
            [
                new("Klinik", vet.ClinicName),
                new("Uzmanlık", vet.Specialty),
                new("Giriş e-postası", vet.Email)
            ],
            Note = "Hekimin randevu, muayene notu veya aşı geçmişi varsa kaydı silinmez, pasif yapılır (yeni randevu alamaz). " +
                   "Geçmişi yoksa hekim kaydı ve giriş hesabı silinir.",
            CancelUrl = Url.Action(nameof(Details), new { id })!
        });
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await _veterinarianService.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Klinik açılır listesi ve klinik bazında gruplanmış hizmet onay kutuları.</summary>
    private async Task LoadOptionsAsync(IEnumerable<int>? selectedServiceIds, CancellationToken cancellationToken)
    {
        var clinics = await _clinicService.GetAllAsync(includeInactive: true, cancellationToken);
        ViewData["Clinics"] = clinics.Select(c => new SelectListItem(c.IsActive ? c.Name : $"{c.Name} (pasif)", c.Id.ToString())).ToList();
        ViewData["Services"] = await _serviceCatalog.GetAllAsync(cancellationToken: cancellationToken);
        ViewData["SelectedServiceIds"] = (selectedServiceIds ?? []).ToHashSet();
    }
}
