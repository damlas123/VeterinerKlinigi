using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using VeterinerKlinigi.API.Contracts;

namespace VeterinerKlinigi.API.Controllers;

/// <summary>
/// Uygulamanın ayakta olduğunu ve katmanların doğru bağlandığını doğrulayan uç.
/// İstek akışı: Web (host) → API controller → Business katmanının kaydettiği TimeProvider.
/// </summary>
[Route("api/system")]
public sealed class SystemController : ApiControllerBase
{
    private readonly IHostEnvironment _environment;
    private readonly TimeProvider _timeProvider;

    // Bağımlılıklar constructor üzerinden DI konteynerinden gelir (Dependency Injection).
    public SystemController(IHostEnvironment environment, TimeProvider timeProvider)
    {
        _environment = environment;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// GET /api/system/status → 200 OK
    /// </summary>
    [HttpGet("status")]
    [ProducesResponseType<SystemStatusResponse>(StatusCodes.Status200OK)]
    public ActionResult<SystemStatusResponse> GetStatus()
    {
        var response = new SystemStatusResponse(
            Status: "Çalışıyor",
            Application: _environment.ApplicationName,
            Environment: _environment.EnvironmentName,
            Framework: RuntimeInformation.FrameworkDescription,
            ServerTime: _timeProvider.GetLocalNow());

        return Ok(response);
    }
}
