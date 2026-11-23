using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;
using VeterinerKlinigi.Web.Models;

namespace VeterinerKlinigi.Web.Controllers;

/// <summary>
/// Herkese açık genel sayfalar.
/// </summary>
public class HomeController : AppController
{
    private readonly IServiceCatalogService _serviceCatalog;
    private readonly IVeterinarianService _veterinarianService;
    private readonly IClinicService _clinicService;

    public HomeController(IServiceCatalogService serviceCatalog, IVeterinarianService veterinarianService, IClinicService clinicService)
    {
        _serviceCatalog = serviceCatalog;
        _veterinarianService = veterinarianService;
        _clinicService = clinicService;
    }

    // GET: /
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var services = await _serviceCatalog.GetAllAsync(cancellationToken: cancellationToken);
        var veterinarians = await _veterinarianService.GetAllAsync(cancellationToken: cancellationToken);
        var clinics = await _clinicService.GetAllAsync(cancellationToken: cancellationToken);

        return View(new HomeViewModel(
            services.GroupBy(s => s.Name).Select(g => g.First()).Take(6).ToList(),
            veterinarians.Take(4).ToList(),
            clinics));
    }

    // GET: /hakkimizda
    [HttpGet("hakkimizda")]
    public async Task<IActionResult> About(CancellationToken cancellationToken)
        => View(await _clinicService.GetAllAsync(cancellationToken: cancellationToken));

    // GET: /iletisim
    [HttpGet("iletisim")]
    public async Task<IActionResult> Contact(CancellationToken cancellationToken)
        => View(await _clinicService.GetAllAsync(cancellationToken: cancellationToken));

    // Program.cs'teki UseExceptionHandler("/Home/Error") beklenmeyen hatalarda buraya yönlendirir.
    // ResponseCache: Hata sayfası tarayıcı önbelleğine alınmasın.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // UseStatusCodePagesWithReExecute: 404, 403 ... durumlarında kullanıcı dostu sayfa.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult HttpStatus(int code)
    {
        var model = code switch
        {
            404 => new StatusCodeViewModel(404, "Sayfa bulunamadı", "Aradığınız sayfa veya kayıt bulunamadı. Adresi kontrol edin ya da ana sayfaya dönün."),
            403 => new StatusCodeViewModel(403, "Erişim reddedildi", "Bu sayfayı görüntüleme yetkiniz yok."),
            401 => new StatusCodeViewModel(401, "Giriş gerekli", "Bu sayfayı görüntülemek için giriş yapmalısınız."),
            _ => new StatusCodeViewModel(code, "Bir sorun oluştu", "İsteğiniz işlenemedi. Lütfen daha sonra tekrar deneyin.")
        };

        Response.StatusCode = code;
        return View(model);
    }
}
