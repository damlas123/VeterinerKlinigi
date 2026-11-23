using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Web.Infrastructure;

namespace VeterinerKlinigi.Web.Areas.Veterinarian.Controllers;

/// <summary>
/// Veteriner: kendisine ait randevular. Onaylar, reddeder ve muayene notu girerek tamamlar.
/// Başka bir hekimin randevusu servis katmanında filtrelendiği için açılamaz (404).
/// </summary>
public class AppointmentsController : VeterinarianAreaController
{
    private readonly IAppointmentService _appointmentService;
    private readonly IExaminationNoteService _noteService;

    public AppointmentsController(IAppointmentService appointmentService, IExaminationNoteService noteService)
    {
        _appointmentService = appointmentService;
        _noteService = noteService;
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

    [HttpPost]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.ApproveAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result, nameof(Details), new { id });
        }

        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Reject(int id, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.GetDetailAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        ViewData["Appointment"] = result.Data!.Appointment;
        return View("RejectAppointment", new RejectAppointmentDto());
    }

    [HttpPost]
    public async Task<IActionResult> Reject(int id, RejectAppointmentDto model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _appointmentService.RejectAsync(id, model.Reason, cancellationToken);
            if (result.IsSuccess)
            {
                ShowSuccess(result.Message);
                return RedirectToAction(nameof(Details), new { id });
            }

            if (result.ErrorType is ErrorType.NotFound or ErrorType.Forbidden)
            {
                return HandleFailure(result);
            }

            ModelState.AddResultError(result, nameof(model.Reason));
        }

        var detail = await _appointmentService.GetDetailAsync(id, cancellationToken);
        if (!detail.IsSuccess)
        {
            return HandleFailure(detail);
        }

        ViewData["Appointment"] = detail.Data!.Appointment;
        return View("RejectAppointment", model);
    }

    // GET: /Veterinarian/Appointments/Complete/5 — muayene notu formu
    [HttpGet]
    public async Task<IActionResult> Complete(int id, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.GetDetailAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result);
        }

        if (!result.Data!.CanComplete)
        {
            ShowError("Bu randevu için muayene notu girilemez (randevu onaylanmış ve saati gelmiş olmalıdır).");
            return RedirectToAction(nameof(Details), new { id });
        }

        ViewData["Appointment"] = result.Data.Appointment;
        return View(new ExaminationNoteSaveDto());
    }

    [HttpPost]
    public async Task<IActionResult> Complete(int id, ExaminationNoteSaveDto model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var result = await _noteService.CompleteAppointmentAsync(id, model, cancellationToken);
            if (result.IsSuccess)
            {
                ShowSuccess(result.Message);
                return RedirectToAction(nameof(Details), new { id });
            }

            if (result.ErrorType is ErrorType.NotFound or ErrorType.Forbidden)
            {
                return HandleFailure(result);
            }

            ModelState.AddResultError(result);
        }

        var detail = await _appointmentService.GetDetailAsync(id, cancellationToken);
        if (!detail.IsSuccess)
        {
            return HandleFailure(detail);
        }

        ViewData["Appointment"] = detail.Data!.Appointment;
        return View(model);
    }
}
