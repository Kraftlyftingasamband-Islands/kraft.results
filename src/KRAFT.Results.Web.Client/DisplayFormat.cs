using System.Globalization;

namespace KRAFT.Results.Web.Client;

public static class DisplayFormat
{
    private static readonly CultureInfo IcelandicCulture = new("is-IS");

    public static string Weight(decimal value) =>
        value.ToString("0.0", IcelandicCulture);

    public static string BodyWeight(decimal value) =>
        value.ToString("0.00", IcelandicCulture);

    public static string Points(decimal value) =>
        value.ToString("0.00", IcelandicCulture);

    public static string Date(DateOnly value) =>
        value.ToString("dd.MM.yyyy", IcelandicCulture);

    public static string Date(DateTime value) =>
        value.ToString("dd.MM.yyyy", IcelandicCulture);

    public static string MonthYear(DateOnly value) =>
        value.ToString("MMM yyyy", IcelandicCulture);

    public static bool TryParseWeight(string? input, out decimal value)
    {
        value = 0m;

        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        string trimmed = input.Trim();

        int commaCount = 0;
        int dotCount = 0;

        foreach (char c in trimmed)
        {
            if (c == ',')
            {
                commaCount++;
            }
            else if (c == '.')
            {
                dotCount++;
            }
        }

        // Reject ambiguous strings with multiple separators (e.g. "1.500,5" — thousands + decimal)
        if (commaCount + dotCount > 1)
        {
            return false;
        }

        // Normalize a lone dot to comma so is-IS parses it as decimal
        string normalized = dotCount == 1 && commaCount == 0
            ? trimmed.Replace('.', ',')
            : trimmed;

        return decimal.TryParse(
            normalized,
            NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            IcelandicCulture,
            out value);
    }
}
