using System.ComponentModel.DataAnnotations;
using System.Reflection;
using VeterinerKlinigi.Entities.Enums;

namespace VeterinerKlinigi.Business.Common;

/// <summary>
/// Enum ve gün adlarının Türkçe karşılıkları.
/// </summary>
public static class DisplayText
{
    private static readonly string[] DayNames =
        ["Pazar", "Pazartesi", "Salı", "Çarşamba", "Perşembe", "Cuma", "Cumartesi"];

    /// <summary>DayOfWeek → "Pazartesi" gibi Türkçe gün adı.</summary>
    public static string Day(DayOfWeek day) => DayNames[(int)day];

    /// <summary>Haftanın günleri Pazartesi'den başlayarak (Türkiye'deki alışkanlık).</summary>
    public static IReadOnlyList<DayOfWeek> WeekDaysFromMonday { get; } =
    [
        DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday,
        DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
    ];

    public static string Status(AppointmentStatus status) => EnumName(status);

    public static string Species(PetSpecies species) => EnumName(species);

    /// <summary>Enum değerinin [Display(Name = "...")] niteliğindeki adını döndürür.</summary>
    public static string EnumName<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var member = typeof(TEnum).GetMember(value.ToString()).FirstOrDefault();
        return member?.GetCustomAttribute<DisplayAttribute>()?.GetName() ?? value.ToString();
    }

    /// <summary>
    /// Avatar için baş harf: "Vet. Hek. Ayşe Yılmaz" → "A". Unvan önekleri atlanır.
    /// </summary>
    public static string Initial(string? fullName)
    {
        var word = (fullName ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(w => !w.EndsWith('.'));
        return string.IsNullOrEmpty(word) ? "?" : word[..1].ToUpper(new System.Globalization.CultureInfo("tr-TR"));
    }

    /// <summary>Doğum tarihinden "3 yaş 2 ay" gibi yaş metni üretir.</summary>
    public static string Age(DateOnly birthDate, DateOnly today)
    {
        if (birthDate > today)
        {
            return "-";
        }

        var months = (today.Year - birthDate.Year) * 12 + today.Month - birthDate.Month;
        if (today.Day < birthDate.Day)
        {
            months--;
        }

        var years = months / 12;
        var remainingMonths = months % 12;

        if (years == 0)
        {
            return remainingMonths == 0 ? "1 aydan küçük" : $"{remainingMonths} aylık";
        }

        return remainingMonths == 0 ? $"{years} yaş" : $"{years} yaş {remainingMonths} ay";
    }
}
