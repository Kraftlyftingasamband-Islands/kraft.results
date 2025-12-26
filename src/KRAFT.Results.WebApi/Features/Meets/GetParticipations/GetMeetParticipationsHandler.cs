using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Mappers;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.GetParticipations;

internal sealed class GetMeetParticipationsHandler(ResultsDbContext dbContext)
{
    public Task<List<MeetParticipation>> Handle(string slug, CancellationToken cancellationToken) =>
        dbContext.Set<Meet>()
        .Where(meet => meet.Slug == slug)
        .SelectMany(meet => meet.Participations.Select(p => new MeetParticipation(
            p.Place,
            $"{p.Athlete.Firstname} {p.Athlete.Lastname}",
            p.Athlete.Slug,
            p.Athlete.Gender == "f" ? "Konur" : "Karlar",
            p.Athlete.DateOfBirth != null ? p.Athlete.DateOfBirth.Value.Year : 0,
            p.AgeCategory != null && p.AgeCategory.TitleShort != null ? p.AgeCategory.Title : string.Empty,
            p.WeightCategory != null ? p.WeightCategory.Title : string.Empty,
            p.Team != null ? p.Team.TitleShort : string.Empty,
            p.Team != null ? p.Team.Slug : string.Empty,
            p.Weight,
            p.Total,
            IpfPoints.Create(p.Meet.IsRaw, p.Athlete.Gender, p.Meet.MeetType.Title, p.Weight, p.Total),
            p.Attempts
                .Where(a => a.Round < 4)
                .Select(a => new MeetAttempt(
                    $"{DisciplineMapper.Map(a.DisciplineId)[0]}{a.Round}",
                    a.Round,
                    a.Weight,
                    a.Good,
                    a.Records.Count != 0)))))
        .ToListAsync(cancellationToken);
}