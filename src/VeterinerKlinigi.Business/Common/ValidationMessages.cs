namespace VeterinerKlinigi.Business.Common;

/// <summary>
/// Data Annotation doğrulama mesajlarının Türkçe kalıpları.
/// {0} alanın görünen adı, {1} ve {2} sınır değerleridir.
/// Aynı mesajlar hem sunucu tarafında hem de istemci tarafında (jQuery Validation) gösterilir.
/// </summary>
public static class ValidationMessages
{
    public const string Required = "{0} alanı zorunludur.";
    public const string MaxLength = "{0} en fazla {1} karakter olabilir.";
    public const string LengthRange = "{0} en az {2}, en fazla {1} karakter olmalıdır.";
    public const string Range = "{0} {1} ile {2} arasında olmalıdır.";
    public const string Email = "Geçerli bir e-posta adresi girin.";
    public const string Phone = "Geçerli bir telefon numarası girin.";
    public const string Compare = "{0} ile {1} aynı olmalıdır.";
}
