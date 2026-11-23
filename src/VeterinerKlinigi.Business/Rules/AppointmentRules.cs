namespace VeterinerKlinigi.Business.Rules;

/// <summary>
/// Randevu zaman hesaplarının SAF (veritabanına dokunmayan) fonksiyonları.
/// Çakışma kontrolü ve uygun saat üretimi burada tek yerde yazılır; AppointmentManager
/// hem randevu oluştururken hem de uygun saatleri listelerken bu fonksiyonları kullanır.
/// Veritabanı gerektirmedikleri için kolayca test edilebilirler.
/// </summary>
public static class AppointmentRules
{
    /// <summary>
    /// İki zaman aralığı çakışıyor mu?  [start1, end1) ile [start2, end2)
    /// Formül: start1 &lt; end2  VE  start2 &lt; end1
    /// Örnek: 10:00-10:30 ile 10:15-10:45 → 10:00 &lt; 10:45 ve 10:15 &lt; 10:30 → ÇAKIŞIR.
    /// Örnek: 10:00-10:30 ile 10:30-11:00 → 10:30 &lt; 10:30 yanlış → ÇAKIŞMAZ (arka arkaya randevu).
    /// </summary>
    public static bool Overlaps(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        => start1 < end2 && start2 < end1;

    /// <summary>[start, end] aralığı, [windowStart, windowEnd] aralığının tamamen içinde mi?</summary>
    public static bool FitsInWindow(TimeOnly start, TimeOnly end, TimeOnly windowStart, TimeOnly windowEnd)
        => start >= windowStart && end <= windowEnd;

    /// <summary>Saat, belirtilen dakika aralığına denk geliyor mu? (15 → 10:00, 10:15 ... evet; 10:07 hayır)</summary>
    public static bool IsAlignedToInterval(DateTime time, int intervalMinutes)
        => time.Second == 0 && time.Millisecond == 0 && (time.Hour * 60 + time.Minute) % intervalMinutes == 0;

    /// <summary>
    /// Bir hekimin bir gündeki boş randevu başlangıç saatlerini üretir.
    /// </summary>
    /// <param name="date">Gün.</param>
    /// <param name="windows">Hekimin o günkü müsaitlik aralıkları (klinik saatleriyle kırpılmış).</param>
    /// <param name="durationMinutes">Hizmet süresi.</param>
    /// <param name="intervalMinutes">Başlangıç saatleri arasındaki adım (15 dk).</param>
    /// <param name="busy">O günkü dolu randevular (başlangıç-bitiş).</param>
    /// <param name="now">Şu an (geçmiş saatler listelenmez).</param>
    public static IReadOnlyList<TimeOnly> GenerateSlots(
        DateOnly date,
        IEnumerable<(TimeOnly Start, TimeOnly End)> windows,
        int durationMinutes,
        int intervalMinutes,
        IReadOnlyCollection<(DateTime Start, DateTime End)> busy,
        DateTime now)
    {
        var slots = new SortedSet<TimeOnly>();

        foreach (var window in windows)
        {
            // Dakika cinsinden çalışıyoruz; TimeOnly gece yarısında başa sardığı için
            // tam sayı aritmetiği daha güvenlidir.
            var windowStart = (int)window.Start.ToTimeSpan().TotalMinutes;
            var windowEnd = (int)window.End.ToTimeSpan().TotalMinutes;

            // İlk adayı aralık değerine yuvarla (09:05 → 09:15).
            var first = (windowStart + intervalMinutes - 1) / intervalMinutes * intervalMinutes;

            for (var minute = first; minute + durationMinutes <= windowEnd; minute += intervalMinutes)
            {
                var start = date.ToDateTime(TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(minute)));
                var end = start.AddMinutes(durationMinutes);

                if (start <= now)
                {
                    continue; // Geçmiş saat
                }

                if (busy.Any(b => Overlaps(start, end, b.Start, b.End)))
                {
                    continue; // Dolu
                }

                slots.Add(TimeOnly.FromDateTime(start));
            }
        }

        return slots.ToList();
    }

    /// <summary>
    /// Müsaitlik aralığını kliniğin çalışma saatleriyle kırpar (kesişimini alır).
    /// Kesişim yoksa null döner.
    /// </summary>
    public static (TimeOnly Start, TimeOnly End)? ClipToClinicHours(TimeOnly start, TimeOnly end, TimeOnly clinicOpen, TimeOnly clinicClose)
    {
        var clippedStart = start > clinicOpen ? start : clinicOpen;
        var clippedEnd = end < clinicClose ? end : clinicClose;
        return clippedStart < clippedEnd ? (clippedStart, clippedEnd) : null;
    }
}
