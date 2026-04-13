using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.AgeCategories;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Eras;
using KRAFT.Results.WebApi.Features.WeightCategories;

namespace KRAFT.Results.WebApi.Features.Records;

internal sealed class Record
{
    public int RecordId { get; private set; }

    public int EraId { get; private set; }

    public int AgeCategoryId { get; private set; }

    public int WeightCategoryId { get; private set; }

    public RecordCategory RecordCategoryId { get; private set; }

    public decimal Weight { get; private set; }

    public DateOnly Date { get; private set; }

    public bool IsStandard { get; private set; }

    public int? AttemptId { get; private set; }

    public bool IsCurrent { get; private set; }

    public bool IsRaw { get; private set; }

    public DateTime CreatedOn { get; private set; }

    public string CreatedBy { get; private set; } = null!;

    public AgeCategory AgeCategory { get; private set; } = null!;

    public Attempt? Attempt { get; private set; }

    public Era Era { get; private set; } = null!;

    public WeightCategory WeightCategory { get; private set; } = null!;

    internal static Record Create(
        int eraId,
        int ageCategoryId,
        int weightCategoryId,
        RecordCategory recordCategoryId,
        decimal weight,
        DateOnly date,
        int attemptId,
        bool isRaw,
        string createdBy)
    {
        return new Record
        {
            EraId = eraId,
            AgeCategoryId = ageCategoryId,
            WeightCategoryId = weightCategoryId,
            RecordCategoryId = recordCategoryId,
            Weight = weight,
            Date = date,
            AttemptId = attemptId,
            IsRaw = isRaw,
            IsCurrent = false,
            IsStandard = false,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = createdBy,
        };
    }

    internal void SetCurrent()
    {
        IsCurrent = true;
    }

    internal void UpdateWeight(decimal weight)
    {
        Weight = weight;
    }

    internal void Demote()
    {
        IsCurrent = false;
    }
}