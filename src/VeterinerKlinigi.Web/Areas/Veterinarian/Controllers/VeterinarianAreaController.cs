using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Entities.Constants;
using VeterinerKlinigi.Web.Controllers;

namespace VeterinerKlinigi.Web.Areas.Veterinarian.Controllers;

/// <summary>Veteriner panelinin taban sınıfı: sadece "Veteriner" rolü erişebilir.</summary>
[Area("Veterinarian")]
[Authorize(Roles = RoleNames.Veterinarian)]
public abstract class VeterinarianAreaController : AppController
{
}
