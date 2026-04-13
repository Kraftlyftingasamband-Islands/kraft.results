using KRAFT.Results.WebApi.Enums;

namespace KRAFT.Results.WebApi.Features.Records.ComputeRecords;

internal sealed record SlotKey(
    int EraId,
    int AgeCategoryId,
    int WeightCategoryId,
    RecordCategory RecordCategory,
    bool IsRaw);