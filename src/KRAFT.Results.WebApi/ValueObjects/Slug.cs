using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.ValueObjects;

internal sealed partial class Slug(string value)
    : ValueObject<string>(value)
{
    internal static readonly Slug Empty = new(string.Empty);

    public static Slug Create(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return Empty;
        }

        input = input.ToLowerInvariant();
        input = RemoveDiacritics(input);
        input = NonAlphanumercsSpacesAndHyphens().Replace(input, string.Empty);
        input = ExtraSpaces().Replace(input, "-");
        input = MultipleHyphens().Replace(input, "-");

        return new Slug(input.Trim());
    }

    private static string RemoveDiacritics(string text)
    {
        string normalizedString = text.Normalize(NormalizationForm.FormD);
        StringBuilder stringBuilder = new();

        foreach (char c in normalizedString)
        {
            UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);

            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex NonAlphanumercsSpacesAndHyphens();

    [GeneratedRegex(@"\s+")]
    private static partial Regex ExtraSpaces();

    [GeneratedRegex(@"-+")]
    private static partial Regex MultipleHyphens();
}