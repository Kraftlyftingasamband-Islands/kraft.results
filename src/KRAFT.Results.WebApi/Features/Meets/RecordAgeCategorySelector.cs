using KRAFT.Results.Contracts;

namespace KRAFT.Results.WebApi.Features.Meets;

internal static class RecordAgeCategorySelector
{
    internal static string? SelectBestLabel(IEnumerable<string?> slugs, string? gender)
    {
        List<string?> slugList = slugs.ToList();
        string? bestSlug = slugList
            .Where(s => s != null)
            .FirstOrDefault(s => s != "open")
            ?? slugList.FirstOrDefault();

        return bestSlug?.ToAgeCategoryLabel(gender);
    }
}