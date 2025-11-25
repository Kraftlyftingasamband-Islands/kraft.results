using KRAFT.Results.Core.Features.Participations;
using KRAFT.Results.Core.Features.Records;

namespace KRAFT.Results.Core.Features.AgeCategories;

internal sealed class AgeCategory
{
    public int AgeCategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string? TitleShort { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? Slug { get; set; }

    public ICollection<Participation> Participations { get; } = [];

    public ICollection<Record> Records { get; } = [];
}