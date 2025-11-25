using KRAFT.Results.Core.Features.Eras;
using KRAFT.Results.Core.Features.WeightCategories;

namespace KRAFT.Results.Core.Features.EraWeightCategories;

internal sealed class EraWeightCategory
{
    public int EraWeightCategoryId { get; set; }

    public int EraId { get; set; }

    public int WeightCategoryId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public DateTime CreatedOn { get; set; }

    public Era Era { get; set; } = null!;

    public WeightCategory WeightCategory { get; set; } = null!;
}