using KRAFT.Results.WebApi.Features.AgeCategories;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Eras;
using KRAFT.Results.WebApi.Features.WeightCategories;

namespace KRAFT.Results.WebApi.Features.Records;

internal sealed class Record
{
    public int RecordId { get; set; }

    public int EraId { get; set; }

    public int AgeCategoryId { get; set; }

    public int WeightCategoryId { get; set; }

    public int RecordCategoryId { get; set; }

    public decimal Weight { get; set; }

    public DateOnly Date { get; set; }

    public bool IsStandard { get; set; }

    public int? AttemptId { get; set; }

    public bool IsCurrent { get; set; }

    public bool IsRaw { get; set; }

    public DateTime CreatedOn { get; set; }

    public string CreatedBy { get; set; } = null!;

    public AgeCategory AgeCategory { get; set; } = null!;

    public Attempt? Attempt { get; set; }

    public Era Era { get; set; } = null!;

    public WeightCategory WeightCategory { get; set; } = null!;
}