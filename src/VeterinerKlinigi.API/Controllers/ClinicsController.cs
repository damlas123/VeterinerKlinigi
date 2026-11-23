using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.API.Controllers;

/// <summary>Klinikler (herkese açık bilgiler).</summary>
[Route("api/clinics")]
[AllowAnonymous]
public sealed class ClinicsController : ApiControllerBase
{
    private readonly IClinicService _clinicService;

    public ClinicsController(IClinicService clinicService)
    {
        _clinicService = clinicService;
    }

    /// <summary>GET /api/clinics — aktif klinikler.</summary>
    [HttpGet]
    [ProducesResponseType<List<ClinicDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ClinicDto>>> GetAll(CancellationToken cancellationToken)
        => Ok(await _clinicService.GetAllAsync(cancellationToken: cancellationToken));

    /// <summary>GET /api/clinics/5</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ClinicDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClinicDto>> GetById(int id, CancellationToken cancellationToken)
        => FromResult(await _clinicService.GetByIdAsync(id, cancellationToken));
}
