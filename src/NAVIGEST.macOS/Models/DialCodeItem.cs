#nullable enable
using System.Linq;

namespace NAVIGEST.macOS.Models;

public sealed class DialCodeItem
{
    private DialCodeItem(string shortCode, string country, string prefix, string flagEmoji)
    {
        ShortCode = (shortCode ?? string.Empty).ToUpperInvariant();
        Country = country ?? string.Empty;
        FlagEmoji = flagEmoji ?? string.Empty;
        NormalizedPrefix = NormalizePrefix(prefix);
    }

    public string ShortCode { get; }
    public string Country { get; }
    public string FlagEmoji { get; }
    public string NormalizedPrefix { get; }

    public string PickerDisplay
    {
        get
        {
            var prefix = NormalizedPrefix;
            var code = ShortCode;

            string baseDisplay = string.IsNullOrWhiteSpace(code)
                ? (string.IsNullOrWhiteSpace(prefix) ? Country : prefix)
                : string.IsNullOrWhiteSpace(prefix) ? code : $"{code} {prefix}";

            if (string.IsNullOrWhiteSpace(baseDisplay))
                baseDisplay = Country;

            return string.IsNullOrEmpty(FlagEmoji) ? baseDisplay : $"{FlagEmoji} {baseDisplay}";
        }
    }

    // Helper property for search
    public string SearchText => $"{Country} {ShortCode} {NormalizedPrefix}".ToLowerInvariant();

    public static DialCodeItem Create(string iso2, string country, string prefix) =>
        new(iso2, country, prefix, BuildFlagEmoji(iso2));

    public static DialCodeItem CreateNoPrefix() => new(string.Empty, "Sem indicativo", string.Empty, "🌐");

    public static DialCodeItem CreateCustom(string prefix) =>
        new(string.Empty, $"Indicativo {NormalizePrefix(prefix)}", prefix, string.Empty);

    public static string NormalizePrefix(string? prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return string.Empty;

        var trimmed = prefix.Trim();
        if (trimmed.StartsWith("00"))
            trimmed = trimmed[2..];

        trimmed = trimmed.TrimStart('+');
        var digits = new string(trimmed.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? string.Empty : "+" + digits;
    }

    private static string BuildFlagEmoji(string countryCode)
    {
        if (string.IsNullOrWhiteSpace(countryCode) || countryCode.Length != 2)
            return string.Empty;

        countryCode = countryCode.ToUpperInvariant();
        int offset = 0x1F1E6;
        int asciiOffset = 0x41;

        int firstChar = char.ConvertToUtf32(countryCode, 0) - asciiOffset + offset;
        int secondChar = char.ConvertToUtf32(countryCode, 1) - asciiOffset + offset;

        return char.ConvertFromUtf32(firstChar) + char.ConvertFromUtf32(secondChar);
    }
}
