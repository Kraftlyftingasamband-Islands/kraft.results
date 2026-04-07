using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.Features.Attempts;
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
                x.Participation.Meet.MeetType.MeetTypeId,
            })
            .OrderBy(x => x.Key.MeetTypeId)
            .Select(x => x
                .OrderByDescending(a => a.Weight)
                .Select(a => new PersonalBest(
                    a.Participation.Meet.IsRaw,
                    IsSingleLift(a.Participation.Meet.MeetType.Title),
                    a.Discipline,
                    a.Weight,
                    a.Participation.WeightCategory.Title,
                    a.Participation.Weight.Value,
                    a.Participation.Meet.Slug,
                    a.Participation.Meet.MeetType.Title,
                    a.Participation.Meet.MeetType.MeetTypeId,
                    a.Participation.Meet.StartDate))
                .First())
            .ToListAsync(cancellationToken);

        List<PersonalBest> bestTotals = await dbContext.Set<Participation>()
            .Where(p => p.Athlete.Slug == slug)
            .Where(p => !p.Disqualified)
            .Where(p => p.Meet.MeetType.Title == "Powerlifting")
            .GroupBy(p => new
            {
                p.Meet.MeetType.MeetTypeId,
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
                    p.Meet.Slug,
                    p.Meet.MeetType.MeetTypeId,
                    p.Meet.StartDate))
                .First())
            .ToListAsync(cancellationToken);

        List<PersonalBest> combined = [.. bestLifts, .. bestTotals];

        return [.. combined
            .OrderBy(x => x.MeetTypeId)
            .Select(x => new AthletePersonalBest(
            x.IsRaw,
            x.IsSingleLiftRecord,
            x.Discipline,
            x.Weight,
            x.WeightCategoryTitle,
            x.BodyWeight,
            x.MeetSlug,
            MapMeetType(x.MeetType),
            DateOnly.FromDateTime(x.MeetDate)))];
    }

    private static bool IsSingleLift(string meetType) => meetType != "Powerlifting";

#pragma warning disable S3358 // Ternary operators should not be nested
    private static string MapMeetType(string type) =>
        !IsSingleLift(type) ? Constants.Powerlifting
        : type == "Benchpress" ? $"{Constants.Bench} ({Constants.SingeLift})"
        : type == "Deadlift" ? $"{Constants.Deadlift} ({Constants.SingeLift})"
        : type;
#pragma warning restore S3358 // Ternary operators should not be nested

    private sealed record class PersonalBest(
        bool IsRaw,
        bool IsSingleLiftRecord,
        Discipline Discipline,
        decimal Weight,
        string WeightCategoryTitle,
        decimal BodyWeight,
        string MeetSlug,
        string MeetType,
        int MeetTypeId,
        DateTime MeetDate);
}