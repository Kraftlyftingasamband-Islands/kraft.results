using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
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
                p.Place,
                Athlete = p.Athlete.Firstname + " " + p.Athlete.Lastname,
                AthleteSlug = p.Athlete.Slug,
                p.Athlete.Gender,
                YearOfBirth = p.Athlete.DateOfBirth != null ? p.Athlete.DateOfBirth.Value.Year : 0,
                HasAgeCategory = p.AgeCategory != null && p.AgeCategory.TitleShort != null,
                AgeCategorySlug = p.AgeCategory != null && p.AgeCategory.TitleShort != null ? p.AgeCategory.Slug : null,
                WeightCategory = p.WeightCategory != null ? p.WeightCategory.Title : string.Empty,
                Club = p.Team != null ? p.Team.TitleShort : string.Empty,
                ClubSlug = p.Team != null ? p.Team.Slug : string.Empty,
                p.Weight,
                p.Total,
                IpfPoints = IpfPoints.Create(p.Meet.IsRaw, p.Athlete.Gender, p.Meet.MeetType.Title, p.Weight, p.Total),
                p.Disqualified,
                GenderDisplay = p.Athlete.Gender == "f" ? "Konur" : "Karlar",
                Attempts = p.Attempts
                    .Where(a => a.Round < 4)
                    .Select(a => new MeetAttempt(
                        a.Discipline,
                        a.Round,
                        a.Weight,
                        a.Good,
                        a.Records.Count != 0)),
            }))
            .ToListAsync(cancellationToken);

        List<MeetParticipation> participations = rows
            .Select(r =>
            {
                string ageCategoryLabel = r.HasAgeCategory && r.AgeCategorySlug != null
                    ? r.AgeCategorySlug.ToAgeCategoryLabel(r.Gender)
                    : string.Empty;
                string ageCategorySlug = r.HasAgeCategory ? r.AgeCategorySlug ?? string.Empty : string.Empty;

                return new MeetParticipation(
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
                    r.Total,
                    r.IpfPoints,
                    r.Disqualified,
                    r.Attempts);
            })
            .ToList();

        return participations
            .OrderBy(p => p.Rank <= 0)
            .ThenBy(p => p.Rank)
            .ThenBy(p => p.Athlete, StringComparer.Ordinal)
            .ToList();
    }
}