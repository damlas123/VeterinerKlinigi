namespace VeterinerKlinigi.Business.Common;

/// <summary>
/// İş katmanındaki metotların ortak dönüş tipi.
/// Beklenen hatalar (geçmiş tarih, çakışma, kayıt bulunamadı ...) exception fırlatılarak değil,
/// bu nesneyle bildirilir. Böylece hata akışı açıkça görünür ve her kanal (MVC/API/MCP)
/// sonucu kendi diline çevirir.
/// </summary>
public class ServiceResult
{
    protected ServiceResult(ErrorType errorType, string? message, string? field)
    {
        ErrorType = errorType;
        Message = message;
        Field = field;
    }

    /// <summary>İşlem başarılı mı?</summary>
    public bool IsSuccess => ErrorType == ErrorType.None;

    /// <summary>Hata türü (başarılıysa None).</summary>
    public ErrorType ErrorType { get; }

    /// <summary>Kullanıcıya gösterilecek Türkçe mesaj (başarı veya hata).</summary>
    public string? Message { get; }

    /// <summary>Hata belirli bir form alanıyla ilgiliyse o alanın adı (formda alanın altında gösterilir).</summary>
    public string? Field { get; }

    public static ServiceResult Success(string? message = null) => new(ErrorType.None, message, null);

    public static ServiceResult Failure(ErrorType errorType, string message, string? field = null)
        => new(errorType, message, field);

    public static ServiceResult Invalid(string message, string? field = null) => Failure(ErrorType.Validation, message, field);

    public static ServiceResult NotFound(string message = "Kayıt bulunamadı.") => Failure(ErrorType.NotFound, message);

    public static ServiceResult Forbidden(string message = "Bu işlem için yetkiniz yok.") => Failure(ErrorType.Forbidden, message);

    public static ServiceResult Conflict(string message, string? field = null) => Failure(ErrorType.Conflict, message, field);
}

/// <summary>
/// Veri döndüren işlemlerin sonucu. Başarılıysa Data doludur.
/// </summary>
public class ServiceResult<T> : ServiceResult
{
    private ServiceResult(ErrorType errorType, string? message, string? field, T? data)
        : base(errorType, message, field)
    {
        Data = data;
    }

    /// <summary>İşlem başarılıysa dönen veri.</summary>
    public T? Data { get; }

    public static ServiceResult<T> Success(T data, string? message = null) => new(ErrorType.None, message, null, data);

    public static new ServiceResult<T> Failure(ErrorType errorType, string message, string? field = null)
        => new(errorType, message, field, default);

    public static new ServiceResult<T> Invalid(string message, string? field = null) => Failure(ErrorType.Validation, message, field);

    public static new ServiceResult<T> NotFound(string message = "Kayıt bulunamadı.") => Failure(ErrorType.NotFound, message);

    public static new ServiceResult<T> Forbidden(string message = "Bu işlem için yetkiniz yok.") => Failure(ErrorType.Forbidden, message);

    public static new ServiceResult<T> Conflict(string message, string? field = null) => Failure(ErrorType.Conflict, message, field);

    /// <summary>Başka bir işlemin hata sonucunu bu tipe taşır.</summary>
    public static ServiceResult<T> FromError(ServiceResult error) => new(error.ErrorType, error.Message, error.Field, default);
}
