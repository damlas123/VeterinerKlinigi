using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Web.Infrastructure;
using VeterinerKlinigi.Web.Models;

namespace VeterinerKlinigi.Web.Areas.Admin.Controllers;

/// <summary>Hizmet yönetimi (CRUD): ad, süre (dk) ve ücret.</summary>
public class ServicesController : AdminController
{
    private readonly IServiceCatalogService _serviceCatalog;
    private readonly IClinicService _clinicService;

    public ServicesController(IServiceCatalogService serviceCatalog, IClinicService clinicService)
    {
        _serviceCatalog = serviceCatalog;
        _clinicService = clinicService;
    }

    public async Task<IActionResult> Index(int? clinicId, CancellationToken cancellationToken)
    {
        ViewData["ClinicId"] = clinicId;
        ViewData["Clinics"] = await ClinicOptionsAsync(clinicId, cancellationToken);
        return View(await _serviceCatalog.GetAllAsync(clinicId, includeInactive: true, cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? clinicId, CancellationToken cancellationToken)
        => await FormViewAsync(new ServiceSaveDto { ClinicId = clinicId ?? 0 }, null, cancellationToken);

    [HttpPost]
    public async Task<IActionResult> Create(ServiceSaveDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await FormViewAsync(model, null, cancellationToken);
        }

        var result = await _serviceCatalog.CreateAsync(model, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddResultError(result);
            return await FormViewAsync(model, null, cancellationToken);
        }

        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await _serviceCatalog.GetForEditAsync(id, cancellationToken);
        return result.IsSuccess ? await FormViewAsync(result.Data!, id, cancellationToken) : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ServiceSaveDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await FormViewAsync(model, id, cancellationToken);
        }

        var result = await _serviceCatalog.UpdateAsync(id, model, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorType == ErrorType.NotFound)
            {
                return NotFound();
            }

            ModelState.AddResultError(result);
            return await FormViewAsync(model, id, cancellationToken);
        }

        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _serviceCatalog.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        var service = result.Data!;
        return View("DeleteConfirm", new DeleteConfirmViewModel
        {
            Id = id.ToString(),
            Title = "Hizmeti sil",
            Question = $"'{service.Name}' hizmetini silmek istediğinize emin misiniz?",
            Details =
            [
                new("Klinik", service.ClinicName),
                new("Süre", $"{service.Duration} dk"),
                new("Ücret", service.Price.ToString("C2"))
            ],
            Note = "Bu hizmete ait randevu varsa hizmet silinmez, pasif yapılır; geçmiş randevular ve ciro raporu korunur.",
            CancelUrl = Url.Action(nameof(Index))!
        });
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await _serviceCatalog.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    private async Task<IActionResult> FormViewAsync(ServiceSaveDto model, int? id, CancellationToken cancellationToken)
    {
        ViewData["Title"] = id is null ? "Yeni hizmet" : "Hizmeti düzenle";
        ViewData["Id"] = id;
        ViewData["Clinics"] = await ClinicOptionsAsync(model.ClinicId, cancellationToken);
        return View("Form", model);
    }

    private async Task<List<SelectListItem>> ClinicOptionsAsync(int? selected, CancellationToken cancellationToken)
    {
        var clinics = await _clinicService.GetAllAsync(includeInactive: true, cancellationToken);
        return clinics
            .Select(c => new SelectListItem(c.IsActive ? c.Name : $"{c.Name} (pasif)", c.Id.ToString(), c.Id == selected))
            .ToList();
    }
}
