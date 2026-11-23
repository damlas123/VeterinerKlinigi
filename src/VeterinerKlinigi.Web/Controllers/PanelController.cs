using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Entities.Constants;

namespace VeterinerKlinigi.Web.Controllers;

/// <summary>"Panelim" bağlantısı: kullanıcıyı rolüne göre doğru panele yönlendirir.</summary>
[Authorize]
public class PanelController : AppController
{
    public IActionResult Index()
    {
        if (User.IsInRole(RoleNames.Admin))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        if (User.IsInRole(RoleNames.Veterinarian))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Veterinarian" });
        }

        if (User.IsInRole(RoleNames.Member))
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Member" });
        }

        return RedirectToAction("Index", "Home");
    }
}
