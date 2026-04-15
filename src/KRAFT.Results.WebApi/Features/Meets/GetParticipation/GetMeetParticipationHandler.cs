using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.GetParticipation;

internal sealed class GetMeetParticipationHandler(ResultsDbContext dbContext)
{
    public async Task<MeetParticipation?> Handle(int meetId, int participationId, CancellationToken cancellationToken)
    {
        var row = await dbContext.Set<Meet>()
            .Where(meet => EF.Property<int>(meet, "MeetId") == meetId)
            .SelectMany(meet => meet.Participations
                .Where(p => p.ParticipationId == participationId)
                .Select(p => new
                {
                    p.ParticipationId,
                    p.MeetId,
                    p.Place,
                    p.AgeCategoryId,
                    p.WeightCategoryId,
                    Athlete = p.Athlete.Firstname + " " + p.Athlete.Lastname,
                    AthleteSlug = p.Athlete.Slug,
                    p.Athlete.Gender,
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
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return null;
        }

        string ageCategoryLabel = row.HasAgeCategory && row.AgeCategorySlug != null
            ? row.AgeCategorySlug.ToAgeCategoryLabel(row.Gender)
            : string.Empty;
        string ageCategorySlug = row.HasAgeCategory ? row.AgeCategorySlug ?? string.Empty : string.Empty;

        decimal ipfPoints = CalculateIpfPoints(
            row.Disqualified,
            row.Category,
            row.IsRaw,
            row.Gender,
            row.Weight,
            row.Total,
            row.Benchpress);

        IEnumerable<MeetAttempt> attempts = row.Attempts
            .Select(a => new MeetAttempt(
                a.Discipline,
                a.Round,
                a.Weight,
                a.Good,
                a.IsRecord,
                false));

        IReadOnlyList<Discipline> disciplines = row.Category.GetDisciplines();
        decimal displayTotal = row.Disqualified ? 0m : ComputeDisplayTotal(disciplines, attempts);

        return new MeetParticipation(
            row.ParticipationId,
            row.MeetId,
            row.Place,
            row.Athlete,
            row.AthleteSlug,
            row.GenderDisplay,
            row.YearOfBirth,
            ageCategoryLabel,
            ageCategorySlug,
            row.WeightCategory,
            row.Club,
            row.ClubSlug,
            row.Weight,
            displayTotal,
            ipfPoints,
            row.Disqualified,
            attempts);
    }

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
}
