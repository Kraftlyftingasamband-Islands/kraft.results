using KRAFT.Results.Contracts.Records;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.EraWeightCategories;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Records.Get;

internal sealed class GetRecordsHandler(ResultsDbContext dbContext)
{
    public async Task<List<RecordGroup>> Handle(
        string gender,
        string ageCategory,
        string equipmentType,
        CancellationToken cancellationToken)
    {
        bool isClassic = string.Equals(equipmentType, "classic", StringComparison.OrdinalIgnoreCase);

        string genderLower = gender.ToLowerInvariant();

        DateTime now = DateTime.UtcNow;

        bool excludeJuniorsOnly = !string.Equals(ageCategory, "junior", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(ageCategory, "subjunior", StringComparison.OrdinalIgnoreCase);

        List<RawRecordData> rawData = await dbContext.Set<Record>()
            .Where(r => r.Era.EndDate.Year > DateTime.UtcNow.Year)
            .Where(r => r.WeightCategory.Gender == Gender.Parse(genderLower))
            .Where(r => r.AgeCategory.Slug == ageCategory)
            .Where(r => r.IsRaw == isClassic)
            .Where(r => r.RecordCategoryId != RecordCategory.TotalWilks && r.RecordCategoryId != RecordCategory.TotalIpfPoints)
            .Where(r => dbContext.Set<EraWeightCategory>()
                .Any(ewc => ewc.EraId == r.EraId
                    && ewc.WeightCategoryId == r.WeightCategoryId
                    && ewc.FromDate < now
                    && ewc.ToDate > now))
            .Where(r => !excludeJuniorsOnly || !r.WeightCategory.JuniorsOnly)
            .OrderBy(r => r.RecordCategoryId)
            .ThenBy(r => r.WeightCategory.MinWeight)
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

        rawData = rawData
            .GroupBy(r => (r.RecordCategoryId, r.WeightCategoryId))
            .Select(g => g
                .OrderByDescending(r => r.Weight)
                .ThenByDescending(r => r.RecordId)
                .First())
            .OrderBy(r => r.RecordCategoryId)
            .ThenBy(r => r.MinWeight)
            .ToList();

        List<RecordGroup> groups = rawData
            .GroupBy(r => r.RecordCategoryId)
            .Select(g => new RecordGroup(
                MapCategoryName(g.Key),
                g.Select(r => new RecordEntry(
                    r.WeightCategory,
                    r.Athlete,
                    r.AthleteSlug,
                    r.BirthYear,
                    r.Club,
                    r.BodyWeight,
                    r.Weight,
                    r.Date,
                    r.Meet,
                    r.MeetSlug,
                    r.IsStandard)).ToList()))
            .ToList();

        return groups;
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