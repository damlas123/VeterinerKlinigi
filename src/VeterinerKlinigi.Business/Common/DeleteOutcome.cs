namespace VeterinerKlinigi.Business.Common;

/// <summary>
/// "Sil" işleminin sonucu. Bağlı kaydı (randevu, aşı, muayene notu) olan kayıtlar
/// geçmiş bilgiler kaybolmasın diye silinmez, arşivlenir (IsActive = false).
/// </summary>
public enum DeleteOutcome
{
    /// <summary>Kayıt veritabanından tamamen silindi.</summary>
    Deleted = 0,

    /// <summary>Kaydın bağlı geçmişi olduğu için arşivlendi (pasif yapıldı).</summary>
    Archived = 1
}
