using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Business.Common;

namespace VeterinerKlinigi.API.Controllers;

/// <summary>
/// Tüm API controller'larının ortak taban sınıfı.
/// İş katmanından dönen ServiceResult'ı doğru HTTP durum koduna çevirir:
///   Validation → 400, Forbidden → 403, NotFound → 404, Conflict → 409
/// Hata cevapları standart "ProblemDetails" (application/problem+json) biçimindedir.
/// </summary>
[ApiController]
// API controller'ları ayrı bir "Api" alanına (area) konur. Böylece MVC sayfalarındaki
// asp-controller="Appointments" asp-action="Approve" gibi bağlantılar, aynı adlı API uçlarına
// (/api/appointments/15/approve) değil, bulunulan panelin action'ına (/Admin/Appointments/Approve/15) gider.
// API adresleri değişmez; area sadece bağlantı üretiminde kullanılan bir rota değeridir.
[Area("Api")]
[Produces("application/json")]
// API'de yazma işlemleri sadece JWT ile yapılabildiği için (ApiPolicies.WriteAccess) CSRF riski yoktur;
// MVC formları için eklenen global anti-forgery filtresi API'ye uygulanmaz.
[IgnoreAntiforgeryToken]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Başarılıysa 200 + veri, değilse uygun hata kodu.</summary>
    protected ActionResult<T> FromResult<T>(ServiceResult<T> result)
        => result.IsSuccess ? Ok(result.Data) : Failure(result);

    /// <summary>Başarılıysa 200 + mesaj, değilse uygun hata kodu.</summary>
    protected ActionResult FromResult(ServiceResult result)
        => result.IsSuccess ? Ok(new { message = result.Message }) : Failure(result);

    /// <summary>ServiceResult hatasını ProblemDetails cevabına çevirir.</summary>
    protected ObjectResult Failure(ServiceResult result)
    {
        var (status, title) = result.ErrorType switch
        {
            ErrorType.Validation => (StatusCodes.Status400BadRequest, "Geçersiz istek"),
            ErrorType.Forbidden => (StatusCodes.Status403Forbidden, "Bu işlem için yetkiniz yok"),
            ErrorType.NotFound => (StatusCodes.Status404NotFound, "Kayıt bulunamadı"),
            ErrorType.Conflict => (StatusCodes.Status409Conflict, "Çakışma"),
            _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen hata")
        };

        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, status, title, detail: result.Message);
        if (result.Field is not null)
        {
            problem.Extensions["field"] = result.Field;
        }

        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
