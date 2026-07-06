using System.Text.RegularExpressions;

namespace SistemaEscolarWeb.Helpers;

public static partial class ChileanFormatHelper
{
    private static readonly Regex RutPattern = RutRegex();
    private static readonly Regex PhonePattern = PhoneRegex();

    public static string NormalizeRut(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var clean = Regex.Replace(value.Trim().ToUpperInvariant(), @"[^0-9K]", string.Empty);
        if (clean.Length < 2)
            return clean;

        return $"{clean[..^1]}-{clean[^1]}";
    }

    public static string NormalizeRutLookupKey(string? value)
    {
        return NormalizeRut(value).Replace("-", string.Empty);
    }

    public static string FormatRutWithDots(string? value)
    {
        var rut = NormalizeRut(value);
        if (!RutPattern.IsMatch(rut))
            return rut;

        var parts = rut.Split('-');
        var numberWithDots = Regex.Replace(parts[0], @"\B(?=(\d{3})+(?!\d))", ".");
        return $"{numberWithDots}-{parts[1]}";
    }

    public static bool IsValidRut(string? value)
    {
        var rut = NormalizeRut(value);
        if (!RutPattern.IsMatch(rut))
            return false;

        var parts = rut.Split('-');
        var number = parts[0];
        var verifier = parts[1][0];

        return CalculateRutVerifier(number) == verifier;
    }

    public static bool HasRutDotsFormat(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return DottedRutPattern().IsMatch(value.Trim().ToUpperInvariant());
    }

    public static string? NormalizePhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var digits = Regex.Replace(value, @"\D", string.Empty);
        if (digits.StartsWith("56", StringComparison.Ordinal))
            digits = digits[2..];

        if (digits.Length > 9)
            digits = digits[^9..];

        return digits.Length == 0 ? null : $"+56{digits}";
    }

    public static bool IsValidPhone(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || PhonePattern.IsMatch(value.Trim());
    }

    private static char CalculateRutVerifier(string number)
    {
        var multiplier = 2;
        var sum = 0;

        for (var i = number.Length - 1; i >= 0; i--)
        {
            sum += (number[i] - '0') * multiplier;
            multiplier = multiplier == 7 ? 2 : multiplier + 1;
        }

        var result = 11 - (sum % 11);
        return result switch
        {
            11 => '0',
            10 => 'K',
            _ => result.ToString()[0]
        };
    }

    [GeneratedRegex(@"^[0-9]{7,8}-[0-9K]$")]
    private static partial Regex RutRegex();

    [GeneratedRegex(@"^[0-9]{1,2}\.[0-9]{3}\.[0-9]{3}-[0-9K]$")]
    private static partial Regex DottedRutPattern();

    [GeneratedRegex(@"^\+56[0-9]{9}$")]
    private static partial Regex PhoneRegex();
}
