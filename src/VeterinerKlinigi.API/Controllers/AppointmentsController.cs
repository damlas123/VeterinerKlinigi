using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.API.Auth;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Entities.Enums;

namespace VeterinerKlinigi.API.Controllers;

/// <summary>
/// Randevular. MVC sayfaları ve MCP ile AYNI servisi (IAppointmentService) kullanır;
/// çakışma kontrolü gibi kurallar burada tekrar yazılmaz.
/// </summary>
[Route("api/appointments")]
public sealed class AppointmentsController : ApiControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }

    /// <summary>GET /api/appointments?status=Pending&amp;from=2026-11-01&amp;to=2026-11-30</summary>
    [HttpGet]
    [Authorize(Policy = ApiPolicies.ReadAccess)]
    [ProducesResponseType<List<AppointmentDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<AppointmentDto>>> GetAll(
        [FromQuery] AppointmentStatus? status,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int? veterinarianId,
        [FromQuery] int? petId,
        CancellationToken cancellationToken)
    {
        var filter = new AppointmentFilter { Status = status, From = from, To = to, VeterinarianId = veterinarianId, PetId = petId };
        return Ok(await _appointmentService.GetListAsync(filter, cancellationToken));
    }

    /// <summary>GET /api/appointments/5</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = ApiPolicies.ReadAccess)]
    [ProducesResponseType<AppointmentDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AppointmentDetailDto>> GetById(int id, CancellationToken cancellationToken)
        => FromResult(await _appointmentService.GetDetailAsync(id, cancellationToken));

    /// <summary>
    /// GET /api/appointments/available-slots?serviceId=1&amp;date=2026-11-25&amp;veterinarianId=2
    /// Seçilen hizmet ve tarih için uygun hekimler ve boş saatler.
    /// </summary>
    [HttpGet("available-slots")]
    [Authorize(Policy = ApiPolicies.ReadAccess)]
    [ProducesResponseType<List<VeterinarianSlotsDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<VeterinarianSlotsDto>>> GetAvailableSlots(
        [FromQuery] int serviceId,
        [FromQuery] DateOnly date,
        [FromQuery] int? veterinarianId,
        [FromQuery] int? excludeAppointmentId,
        CancellationToken cancellationToken)
        => FromResult(await _appointmentService.GetAvailableSlotsAsync(serviceId, date, veterinarianId, excludeAppointmentId, cancellationToken));

    /// <summary>
    /// POST /api/appointments — randevu oluşturur (durum: Pending). SADECE JWT ile.
    /// Başarılı: 201 Created + Location başlığı. Çakışma: 409 Conflict.
    /// </summary>
    [HttpPost]
    [Authorize(Policy = ApiPolicies.WriteAccess)]
    [ProducesResponseType<AppointmentDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> Create([FromBody] AppointmentSaveDto request, CancellationToken cancellationToken)
    {
        var result = await _appointmentService.CreateAsync(request, cancellationToken);
        if (!result.IsSuccess)
        {
            return Failure(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>POST /api/appointments/5/approve — Admin veya randevunun hekimi.</summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Policy = ApiPolicies.WriteAccess)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Approve(int id, CancellationToken cancellationToken)
        => FromResult(await _appointmentService.ApproveAsync(id, cancellationToken));

    /// <summary>POST /api/appointments/5/reject — gövde: { "reason": "..." }</summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Policy = ApiPolicies.WriteAccess)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Reject(int id, [FromBody] RejectAppointmentDto request, CancellationToken cancellationToken)
        => FromResult(await _appointmentService.RejectAsync(id, request.Reason, cancellationToken));

    /// <summary>POST /api/appointments/5/cancel — randevu sahibi veya Admin.</summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Policy = ApiPolicies.WriteAccess)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Cancel(int id, CancellationToken cancellationToken)
        => FromResult(await _appointmentService.CancelAsync(id, cancellationToken));
}
