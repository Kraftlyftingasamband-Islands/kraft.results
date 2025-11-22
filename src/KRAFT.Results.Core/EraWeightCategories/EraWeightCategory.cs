using KRAFT.Results.Core.Eras;
using KRAFT.Results.Core.WeightCategories;

namespace KRAFT.Results.Core.EraWeightCategories;

internal class EraWeightCategory
{
    public int EraWeightCategoryId { get; set; }

    public int EraId { get; set; }

    public int WeightCategoryId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public DateTime CreatedOn { get; set; }

    public virtual Era Era { get; set; } = null!;

    public virtual WeightCategory WeightCategory { get; set; } = null!;
}