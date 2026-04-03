using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.GetParticipations;

internal sealed class GetMeetParticipationsHandler(ResultsDbContext dbContext)
{
    private static readonly int[] BenchMeetTypes = [2, 5];

    public async Task<List<MeetParticipation>> Handle(string slug, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Set<Meet>()
            .Where(meet => meet.Slug == slug)
            .SelectMany(meet => meet.Participations.Select(p => new
            {
                p.ParticipationId,
                p.MeetId,
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
                p.Benchpress,
                IsRaw = meet.IsRaw,
                MeetTypeId = meet.MeetType.MeetTypeId,
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

                decimal ipfPoints = CalculateIpfPoints(r.Disqualified, r.MeetTypeId, r.IsRaw, r.Gender, r.Weight, r.Total, r.Benchpress);

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
                    r.Total,
                    ipfPoints,
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

    private static decimal CalculateIpfPoints(
        bool disqualified,
        int meetTypeId,
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

        string ipfType;
        decimal liftWeight;

        if (BenchMeetTypes.Contains(meetTypeId))
        {
            ipfType = "Benchpress";
            liftWeight = benchpress;
        }
        else if (meetTypeId == 1)
        {
            ipfType = "Powerlifting";
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

        IpfPoints ipfPoints = IpfPoints.Create(isRaw, gender, ipfType, bodyWeight, liftWeight);
        return ipfPoints.Value;
    }
}