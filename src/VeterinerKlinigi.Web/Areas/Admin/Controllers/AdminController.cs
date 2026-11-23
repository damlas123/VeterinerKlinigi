using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Entities.Constants;
using VeterinerKlinigi.Web.Controllers;

namespace VeterinerKlinigi.Web.Areas.Admin.Controllers;

/// <summary>
/// Admin panelindeki tüm controller'ların taban sınıfı.
/// [Area] ve [Authorize(Roles = Admin)] buradan miras alınır: Admin olmayan kullanıcı
/// bu paneldeki hiçbir sayfayı açamaz (rol bazlı yetkilendirme).
/// </summary>
[Area("Admin")]
[Authorize(Roles = RoleNames.Admin)]
public abstract class AdminController : AppController
{
}
