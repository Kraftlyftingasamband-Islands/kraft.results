using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records;

namespace KRAFT.Results.WebApi.Features.AgeCategories;

internal sealed class AgeCategory
{
    internal const int SlugMaxLength = 50;

    public int AgeCategoryId { get; private set; }

    public string Title { get; private set; } = null!;

    public string? TitleShort { get; private set; }

    public DateTime CreatedOn { get; private set; }

    public string? Slug { get; private set; }

    public ICollection<Participation> Participations { get; } = [];

    public ICollection<Record> Records { get; } = [];

    internal static IReadOnlyList<string> ResolveEligibleSlugs(DateOnly? dateOfBirth, DateOnly meetDate)
    {
        string primary = ResolveSlug(dateOfBirth, meetDate);
        return primary switch
        {
            "subjunior" => ["subjunior", "junior", "open"],
            "junior" => ["junior", "open"],
            "masters1" or "masters2" or "masters3" or "masters4" => [primary, "open"],
            _ => ["open"],
        };
    }

    internal static IReadOnlyList<string> GetCascadeSlugs(string slug)
    {
        return slug switch
        {
            "masters4" => ["masters4", "masters3", "masters2", "masters1", "open"],
            "masters3" => ["masters3", "masters2", "masters1", "open"],
            "masters2" => ["masters2", "masters1", "open"],
            "masters1" => ["masters1", "open"],
            "subjunior" => ["subjunior", "junior", "open"],
            "junior" => ["junior", "open"],
            _ => [slug],
        };
    }

    internal static string ResolveSlug(DateOnly? dateOfBirth, DateOnly meetDate)
    {
        if (dateOfBirth is null)
        {
            return "open";
        }

        int age = meetDate.Year - dateOfBirth.Value.Year;

        if (dateOfBirth.Value.Month > meetDate.Month ||
            (dateOfBirth.Value.Month == meetDate.Month && dateOfBirth.Value.Day > meetDate.Day))
        {
            age--;
        }

        return age switch
        {
            <= 18 => "subjunior",
            <= 23 => "junior",
            >= 40 and <= 49 => "masters1",
            >= 50 and <= 59 => "masters2",
            >= 60 and <= 69 => "masters3",
            >= 70 => "masters4",
            _ => "open",
        };
    }
}