namespace VeterinerKlinigi.Business.Common;

/// <summary>
/// Bir iş işleminin neden başarısız olduğunu belirtir.
/// MVC, REST API ve MCP bu değere bakarak hatayı kendi biçimine çevirir
/// (ör. API'de Validation → 400, NotFound → 404, Conflict → 409).
/// </summary>
public enum ErrorType
{
    /// <summary>Hata yok (işlem başarılı).</summary>
    None = 0,

    /// <summary>Gönderilen veri bir iş kuralına uymuyor (ör. geçmiş tarihe randevu).</summary>
    Validation = 1,

    /// <summary>Kayıt yok veya kullanıcının bu kayda erişimi yok.</summary>
    NotFound = 2,

    /// <summary>Kullanıcının bu işlemi yapma yetkisi yok.</summary>
    Forbidden = 3,

    /// <summary>İşlem mevcut bir kayıtla çakışıyor (ör. dolu randevu saati, aynı isimde hizmet).</summary>
    Conflict = 4
}
