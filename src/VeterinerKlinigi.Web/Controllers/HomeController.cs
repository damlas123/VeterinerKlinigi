using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Web.Models;

namespace VeterinerKlinigi.Web.Controllers;

/// <summary>
/// Herkese açık genel sayfalar.
/// </summary>
public class HomeController : Controller
{
    // GET: /  veya  /Home/Index
    public IActionResult Index()
    {
        return View();
    }

    // Program.cs'teki UseExceptionHandler("/Home/Error") beklenmeyen hatalarda buraya yönlendirir.
    // ResponseCache: Hata sayfası tarayıcı önbelleğine alınmasın.
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
