using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Entities.Constants;
using VeterinerKlinigi.Web.Controllers;

namespace VeterinerKlinigi.Web.Areas.Member.Controllers;

/// <summary>Üye (hayvan sahibi) panelinin taban sınıfı: sadece "Uye" rolü erişebilir.</summary>
[Area("Member")]
[Authorize(Roles = RoleNames.Member)]
public abstract class MemberAreaController : AppController
{
}
