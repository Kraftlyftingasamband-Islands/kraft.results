using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Records;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Athletes.GetPersonalBests;

internal static class GetAthletePersonalBestsDbContextExtensions
{
    internal static async Task<ILookup<int, AthleteRecordMatch>> LoadLiftMatchesByAttemptIdAsync(
        this ResultsDbContext dbContext,
        IReadOnlyList<int> attemptIds,
        CancellationToken cancellationToken)
    {
        if (attemptIds.Count == 0)
        {
            return Enumerable
                .Empty<LiftMatchRow>()
                .ToLookup(x => x.AttemptId, x => x.ToMatch());
        }

        List<LiftMatchRow> rows = await dbContext.Set<Record>()
            .AsNoTracking()
            .Where(r => r.AttemptId != null)
            .Where(r => attemptIds.Contains(r.AttemptId!.Value))
            .Where(r => r.IsCurrent)
            .Where(r => r.Era.EndDate.Year > DateTime.UtcNow.Year)
            .Select(r => new LiftMatchRow(
                r.AttemptId!.Value,
                r.AgeCategory.Slug,
                r.AgeCategory.Title,
                r.WeightCategory.Title,
                r.RecordCategoryId,
                r.Attempt!.Participation.Meet.Category != MeetCategory.Powerlifting,
                r.Attempt.Participation.Athlete.Gender))
            .ToListAsync(cancellationToken);

        return rows.ToLookup(r => r.AttemptId, r => r.ToMatch());
    }

    internal static async Task<ILookup<int, AthleteRecordMatch>> LoadTotalMatchesByParticipationIdAsync(
        this ResultsDbContext dbContext,
        IReadOnlyList<int> participationIds,
        CancellationToken cancellationToken)
    {
        if (participationIds.Count == 0)
        {
            return Enumerable
                .Empty<TotalMatchRow>()
                .ToLookup(x => x.ParticipationId, x => x.ToMatch());
        }

        List<TotalMatchRow> rows = await dbContext.Set<Record>()
            .AsNoTracking()
            .Where(r => r.AttemptId != null)
            .Where(r => participationIds.Contains(r.Attempt!.ParticipationId))
            .Where(r => r.RecordCategoryId == RecordCategory.Total)
            .Where(r => r.IsCurrent)
            .Where(r => r.Era.EndDate.Year > DateTime.UtcNow.Year)
            .Select(r => new TotalMatchRow(
                r.Attempt!.ParticipationId,
                r.AgeCategory.Slug,
                r.AgeCategory.Title,
                r.WeightCategory.Title,
                r.Attempt.Participation.Athlete.Gender))
            .ToListAsync(cancellationToken);

        return rows.ToLookup(r => r.ParticipationId, r => r.ToMatch());
    }

    private static string ResolveAgeCategoryLabel(string? slug, string title, string gender)
    {
        if (slug is null)
        {
            return title;
        }

        string label = slug.ToAgeCategoryLabel(gender);
        return label.Length > 0 ? label : title;
    }

    private sealed record LiftMatchRow(
        int AttemptId,
        string? AgeCategorySlug,
        string AgeCategoryTitle,
        string WeightCategory,
        RecordCategory RecordCategoryId,
        bool IsSingleLift,
        string Gender)
    {
        internal AthleteRecordMatch ToMatch() => new(
            ResolveAgeCategoryLabel(AgeCategorySlug, AgeCategoryTitle, Gender),
            WeightCategory,
            IsWithinPowerlifting: (RecordCategoryId == RecordCategory.BenchSingle || RecordCategoryId == RecordCategory.DeadliftSingle) && !IsSingleLift,
            IsSingleLift,
            IsStandaloneDiscipline: RecordCategoryId == RecordCategory.Bench || RecordCategoryId == RecordCategory.Deadlift);
    }

    private sealed record TotalMatchRow(
        int ParticipationId,
        string? AgeCategorySlug,
        string AgeCategoryTitle,
        string WeightCategory,
        string Gender)
    {
        internal AthleteRecordMatch ToMatch() => new(
            ResolveAgeCategoryLabel(AgeCategorySlug, AgeCategoryTitle, Gender),
            WeightCategory,
            IsWithinPowerlifting: false,
            IsSingleLift: false,
            IsStandaloneDiscipline: false);
    }
}
