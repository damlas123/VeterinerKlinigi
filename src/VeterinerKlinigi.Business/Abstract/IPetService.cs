using VeterinerKlinigi.Business.Common;
using VeterinerKlinigi.Business.Dtos;

namespace VeterinerKlinigi.Business.Abstract;

/// <summary>Evcil hayvan işlemleri. Tüm sorgular giriş yapan kullanıcının yetkisine göre filtrelenir.</summary>
public interface IPetService
{
    /// <summary>Üye: kendi hayvanları, Veteriner: hastaları, Admin: tümü.</summary>
    Task<List<PetDto>> GetListAsync(PetFilter? filter = null, CancellationToken cancellationToken = default);

    /// <summary>Hayvanın sağlık geçmişi (aşılar, randevular, muayene notları).</summary>
    Task<ServiceResult<PetDetailDto>> GetDetailAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<PetEditDto>> GetForEditAsync(int id, CancellationToken cancellationToken = default);

    Task<ServiceResult<int>> CreateAsync(PetSaveDto dto, string? photoUrl, CancellationToken cancellationToken = default);

    /// <summary>Başarılıysa artık kullanılmayan eski fotoğrafın yolunu döndürür (Web katmanı diskten siler).</summary>
    Task<ServiceResult<string?>> UpdateAsync(int id, PetSaveDto dto, string? newPhotoUrl, bool removePhoto, CancellationToken cancellationToken = default);

    Task<ServiceResult<PetDeleteResult>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
