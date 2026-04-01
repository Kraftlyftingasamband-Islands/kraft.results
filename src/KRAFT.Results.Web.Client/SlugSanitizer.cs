using System.Text.RegularExpressions;

namespace KRAFT.Results.Web.Client;

internal static partial class SlugSanitizer
{
    internal static string SlugHref(string prefix, string slug) =>
        ValidSlugPattern().IsMatch(slug)
            ? $"{prefix}{slug}"
            : "#";

    [GeneratedRegex(@"^[a-z0-9][a-z0-9-]*$")]
    private static partial Regex ValidSlugPattern();
}