using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Web.Infrastructure;
using VeterinerKlinigi.Web.Models;

namespace VeterinerKlinigi.Web.Areas.Admin.Controllers;

/// <summary>Klinik yönetimi (CRUD).</summary>
public class ClinicsController : AdminController
{
    private readonly IClinicService _clinicService;

    public ClinicsController(IClinicService clinicService)
    {
        _clinicService = clinicService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _clinicService.GetAllAsync(includeInactive: true, cancellationToken));

    [HttpGet]
    public IActionResult Create() => FormView(new ClinicSaveDto(), null);

    [HttpPost]
    public async Task<IActionResult> Create(ClinicSaveDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return FormView(model, null);
        }

        var result = await _clinicService.CreateAsync(model, cancellationToken);
        if (!result.IsSuccess)
        {
            ModelState.AddResultError(result);
            return FormView(model, null);
        }

        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await _clinicService.GetForEditAsync(id, cancellationToken);
        return result.IsSuccess ? FormView(result.Data!, id) : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ClinicSaveDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return FormView(model, id);
        }

        var result = await _clinicService.UpdateAsync(id, model, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorType == ErrorType.NotFound)
            {
                return NotFound();
            }

            ModelState.AddResultError(result);
            return FormView(model, id);
        }

        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _clinicService.GetByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        var clinic = result.Data!;
        return View("DeleteConfirm", new DeleteConfirmViewModel
        {
            Id = id.ToString(),
            Title = "Kliniği sil",
            Question = $"'{clinic.Name}' kliniğini silmek istediğinize emin misiniz?",
            Details =
            [
                new("Adres", clinic.Address),
                new("Aktif veteriner", clinic.VeterinarianCount.ToString()),
                new("Aktif hizmet", clinic.ServiceCount.ToString())
            ],
            Note = "Kliniğe bağlı veteriner veya hizmet kaydı varsa klinik silinmez, pasif yapılır (geçmiş kayıtlar korunur).",
            CancelUrl = Url.Action(nameof(Index))!
        });
    }

    [HttpPost]
    [ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id, CancellationToken cancellationToken)
    {
        var result = await _clinicService.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    private ViewResult FormView(ClinicSaveDto model, int? id)
    {
        ViewData["Title"] = id is null ? "Yeni klinik" : "Kliniği düzenle";
        ViewData["Id"] = id;
        return View("Form", model);
    }
}
