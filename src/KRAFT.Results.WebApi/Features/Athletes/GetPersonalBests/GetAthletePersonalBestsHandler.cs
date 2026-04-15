using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Participations;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Athletes.GetPersonalBests;

internal sealed class GetAthletePersonalBestsHandler(ResultsDbContext dbContext)
{
    public async Task<List<AthletePersonalBest>> Handle(string slug, CancellationToken cancellationToken)
    {
        List<PersonalBest> bestLifts = await dbContext.Set<Attempt>()
            .Where(x => x.Participation.Athlete.Slug == slug)
            .Where(x => !x.Participation.Disqualified)
            .Where(x => x.Good)
            .Where(x => x.Weight > 0)
            .GroupBy(x => new
            {
                x.Discipline,
                x.Participation.Meet.IsRaw,
                Category = (int)x.Participation.Meet.Category,
            })
            .OrderBy(x => x.Key.Category)
            .Select(x => x
                .OrderByDescending(a => a.Weight)
                .Select(a => new PersonalBest(
                    a.Participation.Meet.IsRaw,
                    a.Participation.Meet.Category != MeetCategory.Powerlifting && a.Participation.Meet.Category != MeetCategory.Squat,
                    a.Discipline,
                    a.Weight,
                    a.Participation.WeightCategory.Title,
                    a.Participation.Weight.Value,
                    a.Participation.Meet.Slug,
                    a.Participation.Meet.Category,
                    a.Participation.Meet.StartDate))
                .First())
            .ToListAsync(cancellationToken);

        List<PersonalBest> bestTotals = await dbContext.Set<Participation>()
            .Where(p => p.Athlete.Slug == slug)
            .Where(p => !p.Disqualified)
            .Where(p => p.Meet.Category == MeetCategory.Powerlifting)
            .GroupBy(p => new
            {
                Category = (int)p.Meet.Category,
                p.Meet.IsRaw,
            })
            .Select(g => g
                .OrderByDescending(p => p.Total)
                .Select(p => new PersonalBest(
                    p.Meet.IsRaw,
                    false,
                    Discipline.None,
                    p.Total,
                    p.WeightCategory.Title,
                    p.Weight.Value,
                    p.Meet.Title,
                    p.Meet.Category,
                    p.Meet.StartDate))
                .First())
            .ToListAsync(cancellationToken);

        List<PersonalBest> combined = [.. bestLifts, .. bestTotals];

        return [.. combined
            .OrderBy(x => (int)x.Category)
            .Select(x => new AthletePersonalBest(
            x.IsRaw,
            x.IsSingleLiftRecord,
            x.Discipline,
            x.Weight,
            x.WeightCategoryTitle,
            x.BodyWeight,
            x.MeetSlug,
            x.Category.ToDisplayName(),
            DateOnly.FromDateTime(x.MeetDate)))];
    }

    private sealed record class PersonalBest(
        bool IsRaw,
        bool IsSingleLiftRecord,
        Discipline Discipline,
        decimal Weight,
        string WeightCategoryTitle,
        decimal BodyWeight,
        string MeetSlug,
        MeetCategory Category,
        DateTime MeetDate);
}