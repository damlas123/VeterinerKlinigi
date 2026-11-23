using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Abstract;

namespace VeterinerKlinigi.Web.Areas.Admin.Controllers;

/// <summary>Kullanıcı / hayvan sahibi yönetimi: listeleme, detay, hesap kilitleme.</summary>
public class OwnersController : AdminController
{
    private readonly IUserAdminService _userAdminService;

    public OwnersController(IUserAdminService userAdminService)
    {
        _userAdminService = userAdminService;
    }

    // GET: /Admin/Owners?role=Uye&search=elif
    public async Task<IActionResult> Index(string? role, string? search, CancellationToken cancellationToken)
    {
        ViewData["Role"] = role;
        ViewData["Search"] = search;
        return View(await _userAdminService.GetUsersAsync(role, search, cancellationToken));
    }

    public async Task<IActionResult> Details(string id, CancellationToken cancellationToken)
    {
        var result = await _userAdminService.GetDetailAsync(id, cancellationToken);
        return result.IsSuccess ? View(result.Data) : HandleFailure(result);
    }

    [HttpPost]
    public async Task<IActionResult> Lock(string id, CancellationToken cancellationToken)
        => await SetLockedAsync(id, true, cancellationToken);

    [HttpPost]
    public async Task<IActionResult> Unlock(string id, CancellationToken cancellationToken)
        => await SetLockedAsync(id, false, cancellationToken);

    private async Task<IActionResult> SetLockedAsync(string id, bool locked, CancellationToken cancellationToken)
    {
        var result = await _userAdminService.SetLockedAsync(id, locked, cancellationToken);
        if (result.IsSuccess)
        {
            ShowSuccess(result.Message);
        }
        else
        {
            ShowError(result.Message);
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
