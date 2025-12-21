using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Athletes.GetParticipations;

internal sealed class GetAthleteParticipationsHandler(ResultsDbContext dbContext)
{
    public Task<List<AthleteParticipation>> Handle(string slug, CancellationToken cancellationToken) =>
        dbContext.Set<Participation>()
        .Where(x => x.Athlete.Slug == slug)
        .OrderBy(x => x.Meet.MeetType.MeetTypeId)
        .ThenByDescending(x => x.Meet.StartDate)
        .Select(p => new AthleteParticipation(
            DateOnly.FromDateTime(p.Meet.StartDate),
            $"{p.Meet.Title} {p.Meet.StartDate.Year}",
            p.Meet.Slug,
            MapMeetType(p.Meet.MeetType.Title),
            p.Team != null ? p.Team.TitleShort : null,
            p.Team != null ? p.Team.Slug : null,
            p.Place,
            p.WeightCategory.Title,
            p.Weight,
            p.Squat,
            p.Benchpress,
            p.Deadlift,
            p.Total,
            p.Wilks,
            IpfPoints.Create(p.Meet.IsRaw, p.Athlete.Gender, p.Meet.MeetType.Title, p.Weight, p.Total),
            p.Disqualified))
        .ToListAsync(cancellationToken);

    private static bool IsSingleLift(string meetType) => meetType != "Powerlifting";

#pragma warning disable S3358 // Ternary operators should not be nested
    private static string MapMeetType(string type) =>
        !IsSingleLift(type) ? Constants.Powerlifting
        : type == "Benchpress" ? $"{Constants.Bench} ({Constants.SingeLift})"
        : type == "Deadlift" ? $"{Constants.Deadlift} ({Constants.SingeLift})"
        : type;
#pragma warning restore S3358 // Ternary operators should not be nested
}