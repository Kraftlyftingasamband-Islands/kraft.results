using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Athletes.GetDetails;

internal sealed class GetAthleteDetailsHandler
{
    private readonly ResultsDbContext _dbContext;

    public GetAthleteDetailsHandler(ResultsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AthleteDetails?> Handle(string slug, CancellationToken cancellationToken)
    {
        IReadOnlyList<AthleteParticipation> participations = await GetParticipationsAsync(slug, cancellationToken);

        AthleteDetails? athlete = await _dbContext.Set<Athlete>()
            .Where(x => x.Slug == slug)
            .Select(x => new AthleteDetails(
                x.Slug,
                $"{x.Firstname} {x.Lastname}",
                x.DateOfBirth != null && x.DateOfBirth.Value.Year > 0 ? x.DateOfBirth.Value.Year : null,
                x.Team != null ? x.Team.TitleFull : null,
                x.Team != null ? x.Team.Slug : null,
                0,
                participations))
            .FirstOrDefaultAsync(cancellationToken);

        return athlete;
    }

    private static bool IsSingleLift(string meetType) => meetType != "Powerlifting";

#pragma warning disable S3358 // Ternary operators should not be nested
    private static string MapMeetType(string type) =>
        !IsSingleLift(type) ? Constants.Powerlifting
        : type == "Benchpress" ? $"{Constants.Bench} ({Constants.SingeLift})"
        : type == "Deadlift" ? $"{Constants.Deadlift} ({Constants.SingeLift})"
        : type;
#pragma warning restore S3358 // Ternary operators should not be nested

    private Task<List<AthleteParticipation>> GetParticipationsAsync(string slug, CancellationToken cancellationToken) =>
        _dbContext.Set<Participation>()
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
}