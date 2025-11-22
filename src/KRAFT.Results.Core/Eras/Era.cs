using KRAFT.Results.Core.EraWeightCategories;
using KRAFT.Results.Core.Records;

namespace KRAFT.Results.Core.Eras;

internal sealed class Era
{
    public int EraId { get; set; }

    public string Title { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? Slug { get; set; }

    public ICollection<EraWeightCategory> EraWeightCategories { get; } = [];

    public ICollection<Record> Records { get; } = [];
}