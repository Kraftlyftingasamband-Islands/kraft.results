using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Records;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Eras;
using KRAFT.Results.WebApi.Features.Records;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.GetPendingRecords;

internal sealed class GetMeetPendingRecordsHandler(ResultsDbContext dbContext)
{
    public async Task<List<PendingRecordEntry>?> Handle(string slug, CancellationToken cancellationToken)
    {
        Meet? meet = await dbContext.Set<Meet>()
            .FirstOrDefaultAsync(m => m.Slug == slug, cancellationToken);

        if (meet is null)
        {
            return null;
        }

        if (!meet.RecordsPossible)
        {
            return [];
        }

        DateOnly meetDate = DateOnly.FromDateTime(meet.StartDate);

        Era? era = await dbContext.Set<Era>()
            .Where(e => e.StartDate <= meetDate)
            .Where(e => e.EndDate >= meetDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (era is null)
        {
            return [];
        }

        List<AttemptCandidate> goodAttempts = await dbContext.Set<Attempt>()
            .Where(a => a.Good)
            .Where(a => a.Participation.Meet.Slug == slug)
            .Where(a => a.Discipline != Discipline.None)
            .Select(a => new AttemptCandidate(
                a.AttemptId,
                a.Participation.Athlete.Firstname + " " + a.Participation.Athlete.Lastname,
                a.Discipline,
                a.Weight,
                a.Participation.AgeCategoryId,
                a.Participation.WeightCategoryId,
                a.Participation.AgeCategory.Title,
                a.Participation.WeightCategory.Title))
            .ToListAsync(cancellationToken);

        if (goodAttempts.Count == 0)
        {
            return [];
        }

        HashSet<int> attemptIdsWithRecords = (await dbContext.Set<Record>()
            .Where(r => r.AttemptId != null)
            .Select(r => r.AttemptId!.Value)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        List<RecordSlotMax> currentMaxRecords = await dbContext.Set<Record>()
            .Where(r => r.EraId == era.EraId)
            .GroupBy(r => new
            {
                r.AgeCategoryId,
                r.WeightCategoryId,
                r.RecordCategoryId,
                r.IsRaw,
            })
            .Select(g => new RecordSlotMax(
                g.Key.AgeCategoryId,
                g.Key.WeightCategoryId,
                g.Key.RecordCategoryId,
                g.Key.IsRaw,
                g.Max(r => r.Weight)))
            .ToListAsync(cancellationToken);

        Dictionary<(int AgeCategoryId, int WeightCategoryId, RecordCategory RecordCategoryId, bool IsRaw), decimal> slotMaxLookup =
            currentMaxRecords.ToDictionary(
                r => (r.AgeCategoryId, r.WeightCategoryId, r.RecordCategoryId, r.IsRaw),
                r => r.MaxWeight);

        List<PendingRecordEntry> entries = [];

        foreach (AttemptCandidate attempt in goodAttempts)
        {
            RecordCategory recordCategory = MapDisciplineToRecordCategory(attempt.Discipline);

            if (recordCategory == RecordCategory.None)
            {
                continue;
            }

            if (attemptIdsWithRecords.Contains(attempt.AttemptId))
            {
                continue;
            }

            (int AgeCategoryId, int WeightCategoryId, RecordCategory RecordCategoryId, bool IsRaw) slotKey =
                (attempt.AgeCategoryId, attempt.WeightCategoryId, recordCategory, meet.IsRaw);

            decimal? currentMax = slotMaxLookup.TryGetValue(slotKey, out decimal max) ? max : null;

            if (currentMax.HasValue && attempt.Weight <= currentMax.Value)
            {
                continue;
            }

            entries.Add(new PendingRecordEntry(
                attempt.AttemptId,
                attempt.AthleteName,
                recordCategory.ToDisplayName(),
                attempt.Weight,
                attempt.WeightCategoryTitle,
                attempt.AgeCategoryTitle,
                currentMax));
        }

        return entries;
    }

    private static RecordCategory MapDisciplineToRecordCategory(Discipline discipline) => discipline switch
    {
        Discipline.Squat => RecordCategory.Squat,
        Discipline.Bench => RecordCategory.Bench,
        Discipline.Deadlift => RecordCategory.Deadlift,
        _ => RecordCategory.None,
    };

    private sealed record AttemptCandidate(
        int AttemptId,
        string AthleteName,
        Discipline Discipline,
        decimal Weight,
        int AgeCategoryId,
        int WeightCategoryId,
        string AgeCategoryTitle,
        string WeightCategoryTitle);

    private sealed record RecordSlotMax(
        int AgeCategoryId,
        int WeightCategoryId,
        RecordCategory RecordCategoryId,
        bool IsRaw,
        decimal MaxWeight);
}