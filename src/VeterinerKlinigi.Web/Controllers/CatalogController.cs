using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Business.Dtos;
using VeterinerKlinigi.Web.Models;

namespace VeterinerKlinigi.Web.Controllers;

/// <summary>Herkese açık "Hizmetlerimiz" ve "Veterinerlerimiz" sayfaları.</summary>
public class CatalogController : AppController
{
    private readonly IServiceCatalogService _serviceCatalog;
    private readonly IVeterinarianService _veterinarianService;
    private readonly IClinicService _clinicService;

    public CatalogController(IServiceCatalogService serviceCatalog, IVeterinarianService veterinarianService, IClinicService clinicService)
    {
        _serviceCatalog = serviceCatalog;
        _veterinarianService = veterinarianService;
        _clinicService = clinicService;
    }

    // GET: /hizmetlerimiz?clinicId=1
    [HttpGet("hizmetlerimiz")]
    public async Task<IActionResult> Services(int? clinicId, CancellationToken cancellationToken)
    {
        var clinics = await _clinicService.GetAllAsync(cancellationToken: cancellationToken);
        var services = await _serviceCatalog.GetAllAsync(clinicId, cancellationToken: cancellationToken);
        return View(new ServicesPageViewModel(clinics, services, clinicId));
    }

    // GET: /veterinerlerimiz?clinicId=1&serviceId=2&specialty=cerrahi
    [HttpGet("veterinerlerimiz")]
    public async Task<IActionResult> Veterinarians(int? clinicId, int? serviceId, string? specialty, CancellationToken cancellationToken)
    {
        var filter = new VeterinarianFilter { ClinicId = clinicId, ServiceId = serviceId, Specialty = specialty };
        var model = new VeterinariansPageViewModel(
            await _veterinarianService.GetAllAsync(filter, cancellationToken),
            await _clinicService.GetAllAsync(cancellationToken: cancellationToken),
            await _serviceCatalog.GetAllAsync(clinicId, cancellationToken: cancellationToken),
            await _veterinarianService.GetSpecialtiesAsync(cancellationToken),
            clinicId,
            serviceId,
            specialty);

        return View(model);
    }

    // GET: /veterinerlerimiz/5
    [HttpGet("veterinerlerimiz/{id:int}")]
    public async Task<IActionResult> VeterinarianDetail(int id, CancellationToken cancellationToken)
    {
        var result = await _veterinarianService.GetDetailAsync(id, cancellationToken);
        return result.IsSuccess ? View(result.Data) : NotFound();
    }
}
