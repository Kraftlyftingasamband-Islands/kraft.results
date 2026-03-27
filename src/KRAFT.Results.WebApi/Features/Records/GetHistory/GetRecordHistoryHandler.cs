using KRAFT.Results.Contracts.Records;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Records.GetHistory;

internal sealed class GetRecordHistoryHandler(ResultsDbContext dbContext)
{
    public async Task<RecordHistoryResponse?> Handle(int recordId, CancellationToken cancellationToken)
    {
        RecordKey? key = await dbContext.Set<Record>()
            .Where(r => r.RecordId == recordId)
            .Select(r => new RecordKey(
                r.EraId,
                r.AgeCategoryId,
                r.WeightCategoryId,
                r.RecordCategoryId,
                r.IsRaw,
                r.Era.Title,
                r.AgeCategory.Title ?? string.Empty,
                r.WeightCategory.Title,
                r.WeightCategory.Gender == Gender.Male ? "Karlar" : "Konur"))
            .FirstOrDefaultAsync(cancellationToken);

        if (key is null)
        {
            return null;
        }

        List<RecordHistoryEntry> entries = await dbContext.Set<Record>()
            .Where(r => r.EraId == key.EraId)
            .Where(r => r.AgeCategoryId == key.AgeCategoryId)
            .Where(r => r.WeightCategoryId == key.WeightCategoryId)
            .Where(r => r.RecordCategoryId == key.RecordCategoryId)
            .Where(r => r.IsRaw == key.IsRaw)
            .Where(r => r.Weight > 0)
            .OrderBy(r => r.Date)
            .Select(r => new RecordHistoryEntry(
                r.Date,
                r.Attempt != null ? r.Attempt.Participation.Athlete.Firstname + " " + r.Attempt.Participation.Athlete.Lastname : null,
                r.Attempt != null ? r.Attempt.Participation.Athlete.Slug : null,
                r.Attempt != null ? r.Attempt.Participation.Team!.TitleShort : null,
                r.Weight,
                r.Attempt != null ? r.Attempt.Participation.Weight : (decimal?)null,
                r.Attempt != null ? r.Attempt.Participation.Meet.Title + " " + r.Attempt.Participation.Meet.StartDate.Year : null,
                r.Attempt != null ? r.Attempt.Participation.Meet.Slug : null,
                r.IsCurrent,
                r.IsStandard))
            .ToListAsync(cancellationToken);

        string equipmentType = key.IsRaw ? "Án búnaðar" : "Með búnaði";

        return new RecordHistoryResponse(
            MapCategoryName(key.RecordCategoryId),
            key.WeightCategoryTitle,
            key.AgeCategoryTitle,
            key.GenderLabel,
            equipmentType,
            key.EraTitle,
            entries);
    }

#pragma warning disable S3358 // Ternary operators should not be nested
    private static string MapCategoryName(RecordCategory category) =>
        category == RecordCategory.Squat ? "Hn\u00e9beygja"
        : category == RecordCategory.Bench ? "Bekkpressa"
        : category == RecordCategory.Deadlift ? "R\u00e9ttst\u00f6\u00f0ulyfta"
        : category == RecordCategory.Total ? "Samtala"
        : category == RecordCategory.BenchSingle ? "Bekkpressa (st\u00f6k grein)"
        : category == RecordCategory.DeadliftSingle ? "R\u00e9ttst\u00f6\u00f0ulyfta (st\u00f6k grein)"
        : string.Empty;
#pragma warning restore S3358 // Ternary operators should not be nested

    private sealed record RecordKey(
        int EraId,
        int AgeCategoryId,
        int WeightCategoryId,
        RecordCategory RecordCategoryId,
        bool IsRaw,
        string EraTitle,
        string AgeCategoryTitle,
        string WeightCategoryTitle,
        string GenderLabel);
}