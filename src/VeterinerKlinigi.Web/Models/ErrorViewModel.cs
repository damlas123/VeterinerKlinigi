namespace VeterinerKlinigi.Web.Models;

public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

/// <summary>404, 403 gibi durum kodu sayfası.</summary>
public record StatusCodeViewModel(int Code, string Title, string Message);
