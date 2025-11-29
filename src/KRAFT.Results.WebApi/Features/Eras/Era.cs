using KRAFT.Results.WebApi.Features.EraWeightCategories;
using KRAFT.Results.WebApi.Features.Records;

namespace KRAFT.Results.WebApi.Features.Eras;

internal sealed class Era
{
    public int EraId { get; private set; }

    public string Title { get; private set; } = null!;

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public DateTime CreatedOn { get; private set; }

    public string? Slug { get; private set; }

    public ICollection<EraWeightCategory> EraWeightCategories { get; } = [];

    public ICollection<Record> Records { get; } = [];
}