using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.AgeCategories;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Eras;
using KRAFT.Results.WebApi.Features.Records;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.GetParticipations;

internal sealed class GetMeetParticipationsHandler(ResultsDbContext dbContext)
{
    public async Task<List<MeetParticipation>> Handle(string slug, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Set<Meet>()
            .Where(meet => meet.Slug == slug)
            .SelectMany(meet => meet.Participations.Select(p => new
            {
                p.ParticipationId,
                p.MeetId,
                p.Place,
                p.AgeCategoryId,
                p.WeightCategoryId,
                Athlete = p.Athlete.Firstname + " " + p.Athlete.Lastname,
                AthleteSlug = p.Athlete.Slug,
                p.Athlete.Gender,
                AthleteDoB = p.Athlete.DateOfBirth,
                YearOfBirth = p.Athlete.DateOfBirth != null ? p.Athlete.DateOfBirth.Value.Year : 0,
                HasAgeCategory = p.AgeCategory != null && p.AgeCategory.TitleShort != null,
                AgeCategorySlug = p.AgeCategory != null && p.AgeCategory.TitleShort != null ? p.AgeCategory.Slug : null,
                WeightCategory = p.WeightCategory != null ? p.WeightCategory.Title : string.Empty,
                Club = p.Team != null ? p.Team.TitleShort : string.Empty,
                ClubSlug = p.Team != null ? p.Team.Slug : string.Empty,
                Weight = p.Weight.Value,
                p.Total,
                p.Benchpress,
                IsRaw = meet.IsRaw,
                RecordsPossible = meet.RecordsPossible,
                MeetStartDate = meet.StartDate,
                meet.Category,
                p.Disqualified,
                GenderDisplay = p.Athlete.Gender == "f" ? "Konur" : "Karlar",
                Attempts = p.Attempts
                    .Where(a => a.Round < 4)
                    .Select(a => new
                    {
                        a.AttemptId,
                        a.Discipline,
                        a.Round,
                        a.Weight,
                        a.Good,
                        IsRecord = a.Records.Count != 0,
                    }),
            }))
            .ToListAsync(cancellationToken);

        HashSet<int> pendingAttemptIds = [];

        if (rows.Count > 0 && rows[0].RecordsPossible)
        {
            pendingAttemptIds = await ComputePendingAttemptIds(
                slug,
                rows[0].IsRaw,
                rows[0].MeetStartDate,
                cancellationToken);
        }

        List<MeetParticipation> participations = rows
            .Select(r =>
            {
                string ageCategoryLabel = r.HasAgeCategory && r.AgeCategorySlug != null
                    ? r.AgeCategorySlug.ToAgeCategoryLabel(r.Gender)
                    : string.Empty;
                string ageCategorySlug = r.HasAgeCategory ? r.AgeCategorySlug ?? string.Empty : string.Empty;

                decimal ipfPoints = CalculateIpfPoints(
                    r.Disqualified,
                    r.Category,
                    r.IsRaw,
                    r.Gender,
                    r.Weight,
                    r.Total,
                    r.Benchpress);

                IEnumerable<MeetAttempt> attempts = r.Attempts
                    .Select(a => new MeetAttempt(
                        a.Discipline,
                        a.Round,
                        a.Weight,
                        a.Good,
                        a.IsRecord,
                        pendingAttemptIds.Contains(a.AttemptId)));

                IReadOnlyList<Discipline> disciplines = r.Category.GetDisciplines();
                decimal displayTotal = r.Disqualified ? 0m : ComputeDisplayTotal(disciplines, attempts);

                return new MeetParticipation(
                    r.ParticipationId,
                    r.MeetId,
                    r.Place,
                    r.Athlete,
                    r.AthleteSlug,
                    r.GenderDisplay,
                    r.YearOfBirth,
                    ageCategoryLabel,
                    ageCategorySlug,
                    r.WeightCategory,
                    r.Club,
                    r.ClubSlug,
                    r.Weight,
                    displayTotal,
                    ipfPoints,
                    r.Disqualified,
                    attempts);
            })
            .ToList();

        return participations
            .OrderBy(p => p.Rank <= 0)
            .ThenBy(p => p.Rank)
            .ThenBy(p => p.Athlete, StringComparer.Ordinal)
            .ToList();
    }

    private static RecordCategory MapDisciplineToRecordCategory(Discipline discipline) => discipline switch
    {
        Discipline.Squat => RecordCategory.Squat,
        Discipline.Bench => RecordCategory.Bench,
        Discipline.Deadlift => RecordCategory.Deadlift,
        _ => RecordCategory.None,
    };

    private static decimal ComputeDisplayTotal(IReadOnlyList<Discipline> disciplines, IEnumerable<MeetAttempt> attempts)
    {
        decimal total = 0m;

        foreach (Discipline disc in disciplines)
        {
            decimal best = attempts
                .Where(a => a.Discipline == disc && a.IsGood)
                .Select(a => a.Weight)
                .DefaultIfEmpty(0m)
                .Max();

            if (disciplines.Count > 1 && best == 0)
            {
                return 0m;
            }

            total += best;
        }

        return total;
    }

    private static decimal CalculateIpfPoints(
        bool disqualified,
        MeetCategory category,
        bool isRaw,
        Gender gender,
        decimal bodyWeight,
        decimal total,
        decimal benchpress)
    {
        if (disqualified)
        {
            return 0m;
        }

        MeetCategory ipfCategory;
        decimal liftWeight;

        if (category.IsBenchCategory())
        {
            ipfCategory = MeetCategory.Benchpress;
            liftWeight = benchpress;
        }
        else if (category == MeetCategory.Powerlifting)
        {
            ipfCategory = MeetCategory.Powerlifting;
            liftWeight = total;
        }
        else
        {
            return 0m;
        }

        if (liftWeight <= 0)
        {
            return 0m;
        }

        IpfPoints ipfPoints = IpfPoints.Create(isRaw, gender, ipfCategory, bodyWeight, liftWeight);
        return ipfPoints.Value;
    }

    private async Task<HashSet<int>> ComputePendingAttemptIds(
        string slug,
        bool isRaw,
        DateTime meetStartDate,
        CancellationToken cancellationToken)
    {
        DateOnly meetDate = DateOnly.FromDateTime(meetStartDate);

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
            .Where(a => a.Weight > 0)
            .Where(a => a.Participation.Meet.Slug == slug)
            .Where(a => a.Discipline != Discipline.None)
            .Select(a => new AttemptCandidate(
                a.AttemptId,
                a.Discipline,
                a.Weight,
                a.Participation.AgeCategoryId,
                a.Participation.WeightCategoryId))
            .ToListAsync(cancellationToken);

        HashSet<int> meetAttemptIds = goodAttempts.Select(a => a.AttemptId).ToHashSet();

        HashSet<int> attemptIdsWithRecords = (await dbContext.Set<Record>()
            .Where(r => r.AttemptId != null && meetAttemptIds.Contains(r.AttemptId!.Value))
            .Select(r => r.AttemptId!.Value)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        Dictionary<(int AgeCategoryId, int WeightCategoryId, RecordCategory RecordCategoryId, bool IsRaw), decimal> slotMaxLookup =
            await dbContext.Set<Record>()
                .Where(r => r.EraId == era.EraId)
                .GroupBy(r => new
                {
                    r.AgeCategoryId,
                    r.WeightCategoryId,
                    r.RecordCategoryId,
                    r.IsRaw,
                })
                .Select(g => new
                {
                    g.Key.AgeCategoryId,
                    g.Key.WeightCategoryId,
                    g.Key.RecordCategoryId,
                    g.Key.IsRaw,
                    MaxWeight = g.Max(r => r.Weight),
                })
                .ToDictionaryAsync(
                    r => (r.AgeCategoryId, r.WeightCategoryId, r.RecordCategoryId, r.IsRaw),
                    r => r.MaxWeight,
                    cancellationToken);

        HashSet<int> pendingIds = [];

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
                (attempt.AgeCategoryId, attempt.WeightCategoryId, recordCategory, isRaw);

            decimal? currentMax = slotMaxLookup.TryGetValue(slotKey, out decimal max) ? max : null;

            if (currentMax.HasValue && attempt.Weight <= currentMax.Value)
            {
                continue;
            }

            pendingIds.Add(attempt.AttemptId);
        }

        return pendingIds;
    }

    private sealed record AttemptCandidate(
        int AttemptId,
        Discipline Discipline,
        decimal Weight,
        int AgeCategoryId,
        int WeightCategoryId);
}
