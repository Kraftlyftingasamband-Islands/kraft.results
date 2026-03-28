using KRAFT.Results.Contracts.Records;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.EraWeightCategories;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Records.Get;

internal sealed class GetRecordsHandler(ResultsDbContext dbContext)
{
    private static readonly RecordCategory[] DisplayCategories =
    [
        RecordCategory.Squat,
        RecordCategory.Bench,
        RecordCategory.Deadlift,
        RecordCategory.Total,
        RecordCategory.BenchSingle,
        RecordCategory.DeadliftSingle,
    ];

    public async Task<List<RecordGroup>> Handle(
        string gender,
        string ageCategory,
        string equipmentType,
        int eraId,
        DateTime referenceDate,
        CancellationToken cancellationToken)
    {
        bool isClassic = string.Equals(equipmentType, "classic", StringComparison.OrdinalIgnoreCase);

        string genderLower = gender.ToLowerInvariant();

        bool excludeJuniorsOnly = !string.Equals(ageCategory, "junior", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(ageCategory, "subjunior", StringComparison.OrdinalIgnoreCase);

        // Query 1: Fetch all active weight categories for this era/gender/date
        List<ActiveWeightCategory> activeWeightCategories = await dbContext.Set<EraWeightCategory>()
            .Where(ewc => ewc.EraId == eraId)
            .Where(ewc => ewc.FromDate <= referenceDate)
            .Where(ewc => ewc.ToDate >= referenceDate)
            .Where(ewc => ewc.WeightCategory.Gender == Gender.Parse(genderLower))
            .Where(ewc => !excludeJuniorsOnly || !ewc.WeightCategory.JuniorsOnly)
            .OrderBy(ewc => ewc.WeightCategory.MinWeight)
            .Select(ewc => new ActiveWeightCategory(
                ewc.WeightCategoryId,
                ewc.WeightCategory.Title,
                ewc.WeightCategory.MinWeight))
            .ToListAsync(cancellationToken);

        if (activeWeightCategories.Count == 0)
        {
            return [];
        }

        List<int> activeWeightCategoryIds = activeWeightCategories
            .Select(wc => wc.WeightCategoryId)
            .ToList();

        // Query 2: Fetch all candidate records (no IsCurrent filter)
        List<RawRecordData> candidateRecords = await dbContext.Set<Record>()
            .Where(r => r.Status == RecordStatus.Approved)
            .Where(r => r.EraId == eraId)
            .Where(r => activeWeightCategoryIds.Contains(r.WeightCategoryId))
            .Where(r => r.AgeCategory.Slug == ageCategory)
            .Where(r => r.IsRaw == isClassic)
            .Where(r => r.RecordCategoryId != RecordCategory.TotalWilks && r.RecordCategoryId != RecordCategory.TotalIpfPoints)
            .Select(r => new RawRecordData(
                r.RecordId,
                r.RecordCategoryId,
                r.WeightCategoryId,
                r.WeightCategory.Title,
                r.Attempt != null ? r.Attempt.Participation.Athlete.Firstname + " " + r.Attempt.Participation.Athlete.Lastname : null,
                r.Attempt != null ? r.Attempt.Participation.Athlete.Slug : null,
                r.Attempt != null ? r.Attempt.Participation.Athlete.DateOfBirth!.Value.Year : (int?)null,
                r.Attempt != null ? r.Attempt.Participation.Team!.TitleShort : null,
                r.Attempt != null ? r.Attempt.Participation.Weight : (decimal?)null,
                r.Weight,
                r.Date,
                r.Attempt != null ? r.Attempt.Participation.Meet.Title + " " + r.Attempt.Participation.Meet.StartDate.Year : null,
                r.Attempt != null ? r.Attempt.Participation.Meet.Slug : null,
                r.WeightCategory.MinWeight,
                r.IsStandard))
            .ToListAsync(cancellationToken);

        // C# step: Group by (WeightCategoryId, RecordCategoryId), pick highest Weight (then RecordId as tiebreaker)
        Dictionary<(int WeightCategoryId, RecordCategory RecordCategoryId), RawRecordData> bestRecords = candidateRecords
            .GroupBy(r => (r.WeightCategoryId, r.RecordCategoryId))
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(r => r.Weight)
                    .ThenByDescending(r => r.RecordId)
                    .First());

        // Cross-product: For each record category × each weight category, emit the best record or a placeholder
        List<RecordGroup> groups = DisplayCategories
            .Select(category => new RecordGroup(
                category.ToDisplayName(),
                activeWeightCategories
                    .Select(wc =>
                    {
                        (int WeightCategoryId, RecordCategory RecordCategoryId) key = (wc.WeightCategoryId, category);

                        if (bestRecords.TryGetValue(key, out RawRecordData? record))
                        {
                            return new RecordEntry(
                                record.RecordId,
                                record.WeightCategory,
                                record.Athlete,
                                record.AthleteSlug,
                                record.BirthYear,
                                record.Club,
                                record.BodyWeight,
                                record.Weight,
                                record.Date,
                                record.Meet,
                                record.MeetSlug,
                                record.Athlete is null);
                        }

                        return new RecordEntry(
                            0,
                            wc.Title,
                            null,
                            null,
                            null,
                            null,
                            null,
                            0m,
                            default,
                            null,
                            null,
                            false);
                    })
                    .ToList()))
            .ToList();

        return groups;
    }

    private sealed record ActiveWeightCategory(
        int WeightCategoryId,
        string Title,
        decimal MinWeight);

    private sealed record RawRecordData(
        int RecordId,
        RecordCategory RecordCategoryId,
        int WeightCategoryId,
        string WeightCategory,
        string? Athlete,
        string? AthleteSlug,
        int? BirthYear,
        string? Club,
        decimal? BodyWeight,
        decimal Weight,
        DateOnly Date,
        string? Meet,
        string? MeetSlug,
        decimal MinWeight,
        bool IsStandard);
}