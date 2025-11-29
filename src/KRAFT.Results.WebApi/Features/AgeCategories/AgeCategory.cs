using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records;

namespace KRAFT.Results.WebApi.Features.AgeCategories;

internal sealed class AgeCategory
{
    public int AgeCategoryId { get; private set; }

    public string Title { get; private set; } = null!;

    public string? TitleShort { get; private set; }

    public DateTime CreatedOn { get; private set; }

    public string? Slug { get; private set; }

    public ICollection<Participation> Participations { get; } = [];

    public ICollection<Record> Records { get; } = [];
}