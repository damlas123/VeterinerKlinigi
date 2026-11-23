using Microsoft.AspNetCore.Mvc.ModelBinding;
using VeterinerKlinigi.Business.Common;

namespace VeterinerKlinigi.Web.Infrastructure;

public static class ResultExtensions
{
    /// <summary>
    /// İş katmanından gelen hatayı formdaki ilgili alanın altında gösterir.
    /// Alan belirtilmemişse veya formda yoksa formun üstündeki özet alanında gösterilir.
    /// </summary>
    public static void AddResultError(this ModelStateDictionary modelState, ServiceResult result, params string[] formFields)
    {
        var key = result.Field is not null && (formFields.Length == 0 || formFields.Contains(result.Field))
            ? result.Field
            : string.Empty;

        modelState.AddModelError(key, result.Message ?? "İşlem gerçekleştirilemedi.");
    }
}
