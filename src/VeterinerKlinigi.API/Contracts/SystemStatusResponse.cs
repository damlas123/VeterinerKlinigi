namespace VeterinerKlinigi.API.Contracts;

/// <summary>
/// GET /api/system/status ucunun döndürdüğü cevap.
/// Entity değil, sadece dışarıya göstermek istediğimiz alanları içeren bir DTO'dur.
/// </summary>
/// <param name="Status">Uygulamanın durumu.</param>
/// <param name="Application">Çalışan uygulamanın adı.</param>
/// <param name="Environment">Ortam: Development veya Production.</param>
/// <param name="Framework">Çalışan .NET sürümü.</param>
/// <param name="ServerTime">Sunucunun yerel saati.</param>
public sealed record SystemStatusResponse(
    string Status,
    string Application,
    string Environment,
    string Framework,
    DateTimeOffset ServerTime);
