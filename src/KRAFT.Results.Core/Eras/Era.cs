using KRAFT.Results.Core.EraWeightCategories;
using KRAFT.Results.Core.Records;

namespace KRAFT.Results.Core.Eras;

internal class Era
{
    public int EraId { get; set; }

    public string Title { get; set; } = null!;

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? Slug { get; set; }

    public virtual ICollection<EraWeightCategory> EraWeightCategories { get; } = [];

    public virtual ICollection<Record> Records { get; } = [];
}