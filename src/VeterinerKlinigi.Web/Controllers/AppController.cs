using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Common;

namespace VeterinerKlinigi.Web.Controllers;

/// <summary>
/// Tüm MVC controller'larının ortak taban sınıfı.
/// İşlem sonrası gösterilecek mesajlar (TempData) ve iş katmanı hatalarının
/// sayfaya çevrilmesi burada tek yerde yazılır.
/// </summary>
public abstract class AppController : Controller
{
    /// <summary>Bir sonraki sayfada yeşil bilgi mesajı gösterir.</summary>
    protected void ShowSuccess(string? message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            TempData["Success"] = message;
        }
    }

    /// <summary>Bir sonraki sayfada kırmızı hata mesajı gösterir.</summary>
    protected void ShowError(string? message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = message;
        }
    }

    /// <summary>
    /// İş katmanından gelen hatayı sayfaya çevirir:
    /// NotFound → 404 sayfası (başkasının kaydına erişmeye çalışan da bunu görür),
    /// Forbidden → erişim reddedildi sayfası, diğerleri → mesajla birlikte listeye dönüş.
    /// </summary>
    protected IActionResult HandleFailure(ServiceResult result, string action = "Index", object? routeValues = null)
    {
        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(),
            ErrorType.Forbidden => Forbid(),
            _ => RedirectWithError(result.Message, action, routeValues)
        };
    }

    private RedirectToActionResult RedirectWithError(string? message, string action, object? routeValues)
    {
        ShowError(message);
        return RedirectToAction(action, routeValues);
    }
}
