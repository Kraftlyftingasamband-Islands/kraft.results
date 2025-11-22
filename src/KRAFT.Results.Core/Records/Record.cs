using KRAFT.Results.Core.AgeCategories;
using KRAFT.Results.Core.Attempts;
using KRAFT.Results.Core.Eras;
using KRAFT.Results.Core.WeightCategories;

namespace KRAFT.Results.Core.Records;

internal class Record
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

    public virtual AgeCategory AgeCategory { get; set; } = null!;

    public virtual Attempt? Attempt { get; set; }

    public virtual Era Era { get; set; } = null!;

    public virtual WeightCategory WeightCategory { get; set; } = null!;
}