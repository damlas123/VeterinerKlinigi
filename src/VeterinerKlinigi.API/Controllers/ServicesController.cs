using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.API.Controllers;

/// <summary>Hizmetler (muayene, aşı, kısırlaştırma ...). Herkese açık.</summary>
[Route("api/services")]
[AllowAnonymous]
public sealed class ServicesController : ApiControllerBase
{
    private readonly IServiceCatalogService _serviceCatalog;

    public ServicesController(IServiceCatalogService serviceCatalog)
    {
        _serviceCatalog = serviceCatalog;
    }

    /// <summary>GET /api/services?clinicId=1 — aktif hizmetler (kliniğe göre LINQ filtresi).</summary>
    [HttpGet]
    [ProducesResponseType<List<ServiceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ServiceDto>>> GetAll([FromQuery] int? clinicId, CancellationToken cancellationToken)
        => Ok(await _serviceCatalog.GetAllAsync(clinicId, cancellationToken: cancellationToken));

    /// <summary>GET /api/services/5</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<ServiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ServiceDto>> GetById(int id, CancellationToken cancellationToken)
        => FromResult(await _serviceCatalog.GetByIdAsync(id, cancellationToken));
}
