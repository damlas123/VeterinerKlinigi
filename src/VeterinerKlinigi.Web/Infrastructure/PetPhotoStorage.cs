namespace VeterinerKlinigi.Web.Infrastructure;

/// <summary>Evcil hayvan fotoğraflarını güvenli şekilde diske kaydeder ve siler.</summary>
public interface IPetPhotoStorage
{
    /// <summary>Dosyayı doğrular; geçersizse hata mesajı döner. Geçerliyse null.</summary>
    string? Validate(IFormFile file);

    /// <summary>Dosyayı kaydeder ve tarayıcıdan erişilecek yolunu döndürür (ör. /uploads/pets/abc.jpg).</summary>
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>Daha önce kaydedilmiş bir fotoğrafı siler.</summary>
    void Delete(string? photoUrl);
}

/// <summary>
/// GÜVENLİ DOSYA YÜKLEME:
/// 1) Sadece .jpg .jpeg .png .webp uzantıları kabul edilir,
/// 2) En fazla 2 MB,
/// 3) Uzantıya güvenilmez; dosyanın ilk baytlarındaki İMZA (magic bytes) kontrol edilir
///    (ör. "resim.jpg" adlı bir .exe dosyası reddedilir),
/// 4) Kullanıcının verdiği dosya adı KULLANILMAZ; sunucu rastgele (Guid) bir ad üretir
///    (yol enjeksiyonu "../../" ve XSS riskini önler),
/// 5) Dosyalar sadece wwwroot/uploads/pets klasörüne yazılır ve çalıştırılabilir değildir.
/// </summary>
public class PetPhotoStorage : IPetPhotoStorage
{
    public const long MaxFileSize = 2 * 1024 * 1024;
    private const string UploadFolder = "uploads/pets";

    private static readonly Dictionary<string, byte[][]> AllowedSignatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]],
        [".webp"] = [[0x52, 0x49, 0x46, 0x46]] // "RIFF" (+ 8. bayttan itibaren "WEBP")
    };

    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<PetPhotoStorage> _logger;

    public PetPhotoStorage(IWebHostEnvironment environment, ILogger<PetPhotoStorage> logger)
    {
        _environment = environment;
        _logger = logger;
    }

    public string? Validate(IFormFile file)
    {
        if (file.Length == 0)
        {
            return "Seçilen dosya boş.";
        }

        if (file.Length > MaxFileSize)
        {
            return "Fotoğraf en fazla 2 MB olabilir.";
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedSignatures.TryGetValue(extension, out var signatures))
        {
            return "Sadece JPG, PNG veya WEBP formatında fotoğraf yükleyebilirsiniz.";
        }

        using var stream = file.OpenReadStream();
        var header = new byte[12];
        var read = stream.Read(header, 0, header.Length);

        var signatureOk = signatures.Any(sig => read >= sig.Length && header.AsSpan(0, sig.Length).SequenceEqual(sig));
        if (signatureOk && extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
        {
            signatureOk = read >= 12 && header.AsSpan(8, 4).SequenceEqual("WEBP"u8);
        }

        return signatureOk ? null : "Dosya içeriği bir resim dosyası değil.";
    }

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var error = Validate(file);
        if (error is not null)
        {
            throw new InvalidOperationException(error);
        }

        var folder = Path.Combine(_environment.WebRootPath, UploadFolder);
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}{Path.GetExtension(file.FileName).ToLowerInvariant()}";
        var fullPath = Path.Combine(folder, fileName);

        await using var output = new FileStream(fullPath, FileMode.CreateNew);
        await file.CopyToAsync(output, cancellationToken);

        return $"/{UploadFolder}/{fileName}";
    }

    public void Delete(string? photoUrl)
    {
        if (string.IsNullOrWhiteSpace(photoUrl) || !photoUrl.StartsWith($"/{UploadFolder}/", StringComparison.Ordinal))
        {
            return;
        }

        // Sadece dosya adını al: "../" gibi ifadelerle başka klasöre çıkılamaz.
        var fileName = Path.GetFileName(photoUrl);
        var fullPath = Path.Combine(_environment.WebRootPath, UploadFolder, fileName);

        try
        {
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Fotoğraf silinemedi: {Path}", fullPath);
        }
    }
}
