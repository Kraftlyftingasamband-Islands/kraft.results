using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.ValueObjects;

internal sealed partial class Slug : ValueObject<string>
{
    internal static readonly Slug Empty = new(string.Empty);

    private Slug(string value)
        : base(value)
    {
    }

    public static Slug Create(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return Empty;
        }

        string normalized = input
            .Replace('ð', 'd')
            .Replace("þ", "th", StringComparison.OrdinalIgnoreCase)
            .Replace("æ", "ae", StringComparison.OrdinalIgnoreCase)
            .Normalize(NormalizationForm.FormD);

        StringBuilder stringBuilder = new();

        foreach (char c in normalized)
        {
            UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);

            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        string cleaned = stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        cleaned = cleaned.ToLowerInvariant();
        cleaned = NonAlphanumercsSpacesAndHyphens().Replace(cleaned, string.Empty);
        cleaned = ExtraSpaces().Replace(cleaned, "-");
        cleaned = MultipleHyphens().Replace(cleaned, "-");
        cleaned = cleaned.Trim('-').Trim();

        return new Slug(cleaned);
    }

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex NonAlphanumercsSpacesAndHyphens();

    [GeneratedRegex(@"\s+")]
    private static partial Regex ExtraSpaces();

    [GeneratedRegex(@"-+")]
    private static partial Regex MultipleHyphens();
}