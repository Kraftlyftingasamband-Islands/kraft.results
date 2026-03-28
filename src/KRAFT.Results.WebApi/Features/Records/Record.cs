using KRAFT.Results.WebApi.Abstractions;
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

    public RecordStatus Status { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTime CreatedOn { get; private set; }

    public string CreatedBy { get; private set; } = null!;

    public DateTimeOffset? ModifiedOn { get; private set; }

    public string? ModifiedBy { get; private set; }

    public AgeCategory AgeCategory { get; private set; } = null!;

    public Attempt? Attempt { get; private set; }

    public Era Era { get; private set; } = null!;

    public WeightCategory WeightCategory { get; private set; } = null!;

    internal static Record CreatePending(
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
            Status = RecordStatus.Pending,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = createdBy,
        };
    }

    internal void UpdatePending(int attemptId, decimal weight, DateOnly date)
    {
        AttemptId = attemptId;
        Weight = weight;
        Date = date;
        ModifiedOn = DateTimeOffset.UtcNow;
    }

    internal Result Approve(string modifiedBy)
    {
        if (Status != RecordStatus.Pending)
        {
            return RecordErrors.NotPending;
        }

        Status = RecordStatus.Approved;
        ModifiedBy = modifiedBy;
        ModifiedOn = DateTimeOffset.UtcNow;

        return Result.Success();
    }

    internal Result Reject(string? reason, string modifiedBy)
    {
        if (Status != RecordStatus.Pending)
        {
            return RecordErrors.NotPending;
        }

        Status = RecordStatus.Rejected;
        RejectionReason = reason;
        ModifiedBy = modifiedBy;
        ModifiedOn = DateTimeOffset.UtcNow;

        return Result.Success();
    }
}