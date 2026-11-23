namespace VeterinerKlinigi.Web.Models;

/// <summary>Panellerdeki istatistik kartı (Views/Shared/_StatCard.cshtml).</summary>
/// <param name="Color">pv-icon-green, pv-icon-peach, pv-icon-sky veya pv-icon-lavender.</param>
public record StatCardModel(string Title, string Value, string Icon, string Color, string? Url = null, string? Hint = null);
