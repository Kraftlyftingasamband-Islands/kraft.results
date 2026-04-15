using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Athletes.GetParticipations;

internal sealed class GetAthleteParticipationsHandler(ResultsDbContext dbContext)
{
    public Task<List<AthleteParticipation>> Handle(string slug, CancellationToken cancellationToken) =>
        dbContext.Set<Participation>()
        .Where(x => x.Athlete.Slug == slug)
        .OrderBy(x => (int)x.Meet.Category)
        .ThenByDescending(x => x.Meet.StartDate)
        .Select(p => new AthleteParticipation(
            DateOnly.FromDateTime(p.Meet.StartDate),
            $"{p.Meet.Title} {p.Meet.StartDate.Year}",
            p.Meet.Slug,
            p.Meet.Category.ToDisplayName(),
            p.Team != null ? p.Team.TitleShort : null,
            p.Team != null ? p.Team.Slug : null,
            p.Place,
            p.WeightCategory.Title,
            p.Weight.Value,
            p.Squat,
            p.Benchpress,
            p.Deadlift,
            p.Total,
            p.Wilks,
            IpfPoints.Create(p.Meet.IsRaw, p.Athlete.Gender, p.Meet.Category, p.Weight.Value, p.Total),
            p.Disqualified))
        .ToListAsync(cancellationToken);
}