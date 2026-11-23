using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.API.Auth;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.API.Controllers;

/// <summary>
/// Evcil hayvanlar. Giriş zorunludur; her kullanıcı sadece erişebildiği hayvanları görür
/// (üye: kendi hayvanları, veteriner: hastaları, admin: tümü). Başkasına ait id → 404.
/// </summary>
[Route("api/pets")]
[Authorize(Policy = ApiPolicies.ReadAccess)]
public sealed class PetsController : ApiControllerBase
{
    private readonly IPetService _petService;
    private readonly IVaccinationService _vaccinationService;

    public PetsController(IPetService petService, IVaccinationService vaccinationService)
    {
        _petService = petService;
        _vaccinationService = vaccinationService;
    }

    /// <summary>GET /api/pets?search=pam</summary>
    [HttpGet]
    [ProducesResponseType<List<PetDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<PetDto>>> GetAll([FromQuery] string? search, CancellationToken cancellationToken)
        => Ok(await _petService.GetListAsync(new PetFilter { Search = search }, cancellationToken));

    /// <summary>GET /api/pets/5 — sağlık geçmişi (aşılar, randevular, muayene notları).</summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType<PetDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PetDetailDto>> GetById(int id, CancellationToken cancellationToken)
        => FromResult(await _petService.GetDetailAsync(id, cancellationToken));

    /// <summary>GET /api/pets/5/vaccinations — aşı geçmişi, yaklaşan ve geciken aşılar.</summary>
    [HttpGet("{id:int}/vaccinations")]
    [ProducesResponseType<VaccinationScheduleDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VaccinationScheduleDto>> GetVaccinations(int id, CancellationToken cancellationToken)
        => FromResult(await _vaccinationService.GetScheduleByPetAsync(id, cancellationToken));
}
