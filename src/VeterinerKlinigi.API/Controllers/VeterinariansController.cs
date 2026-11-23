using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.API.Controllers;

/// <summary>Veteriner hekimler. Herkese açık.</summary>
[Route("api/veterinarians")]
[AllowAnonymous]
public sealed class VeterinariansController : ApiControllerBase
{
    private readonly IVeterinarianService _veterinarianService;

    public VeterinariansController(IVeterinarianService veterinarianService)
    {
        _veterinarianService = veterinarianService;
    }

    /// <summary>
    /// GET /api/veterinarians?clinicId=1&amp;serviceId=2&amp;specialty=cerrahi
    /// Tüm filtreler opsiyoneldir; LINQ ile sorguya eklenir.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<List<VeterinarianDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<VeterinarianDto>>> GetAll(
        [FromQuery] int? clinicId,
        [FromQuery] int? serviceId,
        [FromQuery] string? specialty,
        CancellationToken cancellationToken)
    {
        var filter = new VeterinarianFilter { ClinicId = clinicId, ServiceId = serviceId, Specialty = specialty };
        return Ok(await _veterinarianService.GetAllAsync(filter, cancellationToken));
    }

    /// <summary>GET /api/veterinarians/5 — hekim, verdiği hizmetler ve haftalık çalışma saatleri.</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<VeterinarianDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VeterinarianDetailDto>> GetById(int id, CancellationToken cancellationToken)
        => FromResult(await _veterinarianService.GetDetailAsync(id, cancellationToken));
}
