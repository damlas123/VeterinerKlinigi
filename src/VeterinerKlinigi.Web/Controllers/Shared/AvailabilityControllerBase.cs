using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Web.Infrastructure;

namespace VeterinerKlinigi.Web.Controllers.Shared;

/// <summary>
/// Müsaitlik ekleme/düzenleme/silme işlemleri Admin ve Veteriner panelinde AYNIDIR.
/// Kod tekrarı olmasın diye ortak taban sınıfta yazıldı; her panel sadece
/// "işlem bitince nereye dönülecek?" sorusunu cevaplar. Yetki kontrolü servis katmanındadır
/// (veteriner sadece kendi saatlerini değiştirebilir).
/// Görünüm: Views/Shared/AvailabilityForm.cshtml (iki panel de aynı formu kullanır).
/// </summary>
public abstract class AvailabilityControllerBase : AppController
{
    protected AvailabilityControllerBase(IAvailabilityService availabilityService)
    {
        AvailabilityService = availabilityService;
    }

    protected IAvailabilityService AvailabilityService { get; }

    /// <summary>İşlem sonrası dönülecek sayfa.</summary>
    protected abstract IActionResult BackToList(int veterinarianId);

    /// <summary>Formdaki "Vazgeç" bağlantısının adresi.</summary>
    protected abstract string CancelUrl(int veterinarianId);

    protected IActionResult FormView(AvailabilitySaveDto model, int? id)
    {
        ViewData["Title"] = id is null ? "Müsaitlik aralığı ekle" : "Müsaitlik aralığını düzenle";
        ViewData["Id"] = id;
        ViewData["CancelUrl"] = CancelUrl(model.VeterinarianId);
        ViewData["Days"] = DisplayText.WeekDaysFromMonday
            .Select(d => new SelectListItem(DisplayText.Day(d), d.ToString(), d == model.DayOfWeek))
            .ToList();
        return View("AvailabilityForm", model);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AvailabilitySaveDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return FormView(model, null);
        }

        var result = await AvailabilityService.CreateAsync(model, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorType == ErrorType.Forbidden)
            {
                return Forbid();
            }

            ModelState.AddResultError(result);
            return FormView(model, null);
        }

        ShowSuccess(result.Message);
        return BackToList(model.VeterinarianId);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await AvailabilityService.GetForEditAsync(id, cancellationToken);
        return result.IsSuccess ? FormView(result.Data!, id) : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, AvailabilitySaveDto model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return FormView(model, id);
        }

        var result = await AvailabilityService.UpdateAsync(id, model, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorType is ErrorType.NotFound or ErrorType.Forbidden)
            {
                return HandleFailure(result);
            }

            ModelState.AddResultError(result);
            return FormView(model, id);
        }

        ShowSuccess(result.Message);
        return BackToList(model.VeterinarianId);
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await AvailabilityService.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        ShowSuccess(result.Message);
        return BackToList(result.Data);
    }
}
