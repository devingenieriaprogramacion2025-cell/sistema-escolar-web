using System.Text.RegularExpressions;
using System.Globalization;
using System.Text;

namespace SistemaEscolarWeb.Helpers;

public static class InputValidationHelper
{
    private static readonly Regex SafeTextPattern = new(@"^[\p{L}0-9 .,;:#()¿?¡!%°&'""/\r\n_-]+$", RegexOptions.Compiled);

    public static bool IsSafeText(string? value, int maxLength, bool required)
    {
        if (string.IsNullOrWhiteSpace(value))
            return !required;

        var text = value.Trim();
        return text.Length <= maxLength && SafeTextPattern.IsMatch(text);
    }

    public static string NormalizeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        var text = Regex.Replace(builder.ToString().Normalize(NormalizationForm.FormC).ToUpperInvariant(), @"[^\p{L}0-9 ]+", " ");
        text = Regex.Replace(text, @"\bDE\b", " ");
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}
