using KRAFT.Results.WebApi.Features.Eras;
using KRAFT.Results.WebApi.Features.WeightCategories;

namespace KRAFT.Results.WebApi.Features.EraWeightCategories;

internal sealed class EraWeightCategory
{
    public int EraWeightCategoryId { get; private set; }

    public int EraId { get; private set; }

    public int WeightCategoryId { get; private set; }

    public DateTime? FromDate { get; private set; }

    public DateTime? ToDate { get; private set; }

    public DateTime CreatedOn { get; private set; }

    public Era Era { get; private set; } = null!;

    public WeightCategory WeightCategory { get; private set; } = null!;
}