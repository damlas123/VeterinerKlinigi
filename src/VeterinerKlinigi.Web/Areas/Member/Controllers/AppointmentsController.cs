using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Web.Infrastructure;
using VeterinerKlinigi.Web.Models;

namespace VeterinerKlinigi.Web.Areas.Member.Controllers;

/// <summary>
/// Üye: randevu alma, takip, düzenleme ve iptal.
/// Uygun saatler sayfadaki jQuery koduyla REST API'den alınır (GET /api/appointments/available-slots);
/// randevu kaydı ise bu controller → IAppointmentService.CreateAsync ile yapılır.
/// Her iki yol da AYNI servis metotlarını kullandığı için kurallar tektir.
/// </summary>
public class AppointmentsController : MemberAreaController
{
    private readonly IAppointmentService _appointmentService;
    private readonly IPetService _petService;
    private readonly IServiceCatalogService _serviceCatalog;

    public AppointmentsController(IAppointmentService appointmentService, IPetService petService, IServiceCatalogService serviceCatalog)
    {
        _appointmentService = appointmentService;
        _petService = petService;
        _serviceCatalog = serviceCatalog;
    }

    public async Task<IActionResult> Index([FromQuery] AppointmentFilter filter, CancellationToken cancellationToken)
    {
        ViewData["Filter"] = filter;
        return View(await _appointmentService.GetListAsync(filter, cancellationToken));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.GetDetailAsync(id, cancellationToken);
        return result.IsSuccess ? View("AppointmentDetails", result.Data) : HandleFailure(result);
    }

    // GET: /Member/Appointments/Create?petId=3
    [HttpGet]
    public async Task<IActionResult> Create(int? petId, CancellationToken cancellationToken)
    {
        var model = new AppointmentFormViewModel
        {
            PetId = petId ?? 0,
            Date = DateOnly.FromDateTime(DateTime.Today).AddDays(1)
        };

        return await FormViewAsync(model, cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Create(AppointmentFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return await FormViewAsync(model, cancellationToken);
        }

        var result = await _appointmentService.CreateAsync(ToDto(model), cancellationToken);
        if (!result.IsSuccess)
        {
            // Çakışma, müsait olmayan saat gibi hatalar formun üstünde açıklamasıyla gösterilir.
            ModelState.AddResultError(result, nameof(model.PetId), nameof(model.ServiceId), nameof(model.OwnerNote));
            model.Time = null;
            return await FormViewAsync(model, cancellationToken);
        }

        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Details), new { id = result.Data!.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.GetForEditAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        var dto = result.Data!;
        var model = new AppointmentFormViewModel
        {
            Id = id,
            PetId = dto.PetId,
            ServiceId = dto.ServiceId,
            VeterinarianId = dto.VeterinarianId,
            Date = DateOnly.FromDateTime(dto.AppointmentDate),
            Time = dto.AppointmentDate.ToString("HH:mm"),
            OwnerNote = dto.OwnerNote
        };

        return await FormViewAsync(model, cancellationToken);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, AppointmentFormViewModel model, CancellationToken cancellationToken)
    {
        model.Id = id;
        if (!ModelState.IsValid)
        {
            return await FormViewAsync(model, cancellationToken);
        }

        var result = await _appointmentService.UpdateAsync(id, ToDto(model), cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.ErrorType == ErrorType.NotFound)
            {
                return HandleFailure(result);
            }

            ModelState.AddResultError(result, nameof(model.PetId), nameof(model.ServiceId), nameof(model.OwnerNote));
            model.Time = null;
            return await FormViewAsync(model, cancellationToken);
        }

        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.CancelAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result, nameof(Details), new { id });
        }

        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Details), new { id });
    }

    private static AppointmentSaveDto ToDto(AppointmentFormViewModel model)
    {
        var time = TimeOnly.ParseExact(model.Time!, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        return new AppointmentSaveDto
        {
            PetId = model.PetId,
            ServiceId = model.ServiceId,
            VeterinarianId = model.VeterinarianId,
            AppointmentDate = model.Date.ToDateTime(time),
            OwnerNote = model.OwnerNote
        };
    }

    private async Task<IActionResult> FormViewAsync(AppointmentFormViewModel model, CancellationToken cancellationToken)
    {
        ViewData["Title"] = model.Id is null ? "Yeni randevu" : "Randevuyu düzenle";
        ViewData["Pets"] = await _petService.GetListAsync(null, cancellationToken);
        ViewData["Services"] = await _serviceCatalog.GetAllAsync(cancellationToken: cancellationToken);
        return View("Form", model);
    }
}
