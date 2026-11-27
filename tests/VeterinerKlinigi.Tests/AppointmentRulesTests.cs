using VeterinerKlinigi.Business.Rules;

namespace VeterinerKlinigi.Tests;

/// <summary>Randevu zaman hesaplarının (çakışma, uygun saat) saf fonksiyon testleri.</summary>
public class AppointmentRulesTests
{
    private static DateTime At(int hour, int minute) => new(2026, 12, 2, hour, minute, 0);

    [Theory]
    [InlineData(10, 15, 10, 45, true)]   // 10:00-10:30 ile 10:15-10:45 kesişir
    [InlineData(9, 45, 10, 15, true)]    // baştan taşan aralık
    [InlineData(10, 5, 10, 20, true)]    // tamamen içinde
    [InlineData(10, 30, 11, 0, false)]   // arka arkaya: 10:30'da başlayan çakışmaz
    [InlineData(9, 30, 10, 0, false)]    // hemen önce biten çakışmaz
    public void Overlaps_ExistingTenToTenThirty(int sh, int sm, int eh, int em, bool expected)
    {
        Assert.Equal(expected, AppointmentRules.Overlaps(At(sh, sm), At(eh, em), At(10, 0), At(10, 30)));
    }

    [Theory]
    [InlineData(9, 0, 9, 30, true)]
    [InlineData(16, 30, 17, 0, true)]    // aralığın sonuna tam oturuyor
    [InlineData(16, 45, 17, 15, false)]  // bitiş, çalışma saatini aşıyor
    [InlineData(8, 45, 9, 15, false)]
    public void FitsInWindow_NineToFive(int sh, int sm, int eh, int em, bool expected)
    {
        Assert.Equal(expected, AppointmentRules.FitsInWindow(new TimeOnly(sh, sm), new TimeOnly(eh, em), new TimeOnly(9, 0), new TimeOnly(17, 0)));
    }

    [Theory]
    [InlineData(10, 0, true)]
    [InlineData(10, 45, true)]
    [InlineData(10, 7, false)]
    [InlineData(10, 20, false)]
    public void IsAlignedToInterval_FifteenMinutes(int hour, int minute, bool expected)
    {
        Assert.Equal(expected, AppointmentRules.IsAlignedToInterval(At(hour, minute), 15));
    }

    [Fact]
    public void GenerateSlots_SkipsSlotsThatWouldOverlapByServiceDuration()
    {
        var windows = new[] { (new TimeOnly(9, 0), new TimeOnly(11, 0)) };
        var busy = new List<(DateTime, DateTime)> { (At(10, 0), At(10, 30)) };

        var slots = AppointmentRules.GenerateSlots(new DateOnly(2026, 12, 2), windows, 30, 15, busy, At(0, 0));

        // 30 dakikalık hizmet: 09:45 başlangıcı 10:15'te biter → çakışır; 10:00 ve 10:15 dolu; 10:30 boş.
        Assert.Equal(["09:00", "09:15", "09:30", "10:30"], slots.Select(s => s.ToString("HH:mm")).ToArray());
    }

    [Fact]
    public void GenerateSlots_DoesNotListPastTimes()
    {
        var windows = new[] { (new TimeOnly(9, 0), new TimeOnly(10, 0)) };

        var slots = AppointmentRules.GenerateSlots(new DateOnly(2026, 12, 2), windows, 30, 15, [], now: At(9, 15));

        Assert.Equal(["09:30"], slots.Select(s => s.ToString("HH:mm")).ToArray());
    }

    [Fact]
    public void GenerateSlots_LongServiceMustFitInsideWindow()
    {
        var windows = new[] { (new TimeOnly(9, 0), new TimeOnly(10, 30)) };

        var slots = AppointmentRules.GenerateSlots(new DateOnly(2026, 12, 2), windows, 120, 15, [], At(0, 0));

        Assert.Empty(slots); // 120 dakikalık hizmet 1,5 saatlik aralığa sığmaz
    }

    [Fact]
    public void ClipToClinicHours_ReturnsIntersectionOrNull()
    {
        Assert.Equal((new TimeOnly(9, 0), new TimeOnly(17, 0)),
            AppointmentRules.ClipToClinicHours(new TimeOnly(8, 0), new TimeOnly(20, 0), new TimeOnly(9, 0), new TimeOnly(17, 0)));
        Assert.Null(AppointmentRules.ClipToClinicHours(new TimeOnly(18, 0), new TimeOnly(20, 0), new TimeOnly(9, 0), new TimeOnly(17, 0)));
    }
}
