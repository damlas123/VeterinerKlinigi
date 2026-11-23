using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Entities.Enums;
using VeterinerKlinigi.Web.Infrastructure;

namespace VeterinerKlinigi.Web.Areas.Admin.Controllers;

/// <summary>Admin: tüm randevular, onay / ret / iptal / silme.</summary>
public class AppointmentsController : AdminController
{
    private readonly IAppointmentService _appointmentService;
    private readonly IClinicService _clinicService;
    private readonly IVeterinarianService _veterinarianService;

    public AppointmentsController(IAppointmentService appointmentService, IClinicService clinicService, IVeterinarianService veterinarianService)
    {
        _appointmentService = appointmentService;
        _clinicService = clinicService;
        _veterinarianService = veterinarianService;
    }

    // GET: /Admin/Appointments?Status=Pending&ClinicId=1&VeterinarianId=2&From=2026-11-01&To=2026-11-30
    public async Task<IActionResult> Index([FromQuery] AppointmentFilter filter, CancellationToken cancellationToken)
    {
        ViewData["Filter"] = filter;
        ViewData["Clinics"] = (await _clinicService.GetAllAsync(includeInactive: true, cancellationToken))
            .Select(c => new SelectListItem(c.Name, c.Id.ToString(), c.Id == filter.ClinicId)).ToList();
        ViewData["Veterinarians"] = (await _veterinarianService.GetAllAsync(new VeterinarianFilter { IncludeInactive = true }, cancellationToken))
            .Select(v => new SelectListItem(v.FullName, v.Id.ToString(), v.Id == filter.VeterinarianId)).ToList();
        return View(await _appointmentService.GetListAsync(filter, cancellationToken));
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.GetDetailAsync(id, cancellationToken);
        return result.IsSuccess ? View("AppointmentDetails", result.Data) : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Approve(int id, CancellationToken cancellationToken)
        => AfterAction(await _appointmentService.ApproveAsync(id, cancellationToken), id);

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

            if (result.ErrorType == ErrorType.NotFound)
            {
                return NotFound();
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

    [HttpPost]
    public async Task<IActionResult> Cancel(int id, CancellationToken cancellationToken)
        => AfterAction(await _appointmentService.CancelAsync(id, cancellationToken), id);

    [HttpPost]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.DeleteAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleFailure(result, nameof(Details), new { id });
        }

        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Index));
    }

    private IActionResult AfterAction(ServiceResult result, int id)
    {
        if (!result.IsSuccess)
        {
            return HandleFailure(result, nameof(Details), new { id });
        }

        ShowSuccess(result.Message);
        return RedirectToAction(nameof(Details), new { id });
    }
}
