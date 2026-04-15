using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.AgeCategories;
using KRAFT.Results.WebApi.Features.Attempts;
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
                meet.Category,
                p.Disqualified,
                GenderDisplay = p.Athlete.Gender == "f" ? "Konur" : "Karlar",
                Attempts = p.Attempts
                    .Where(a => a.Round < 4)
                    .Select(a => new
                    {
                        a.Discipline,
                        a.Round,
                        a.Weight,
                        a.Good,
                        IsRecord = a.Records.Count != 0,
                        RecordAgeCategorySlugs = a.Records.Select(r => r.AgeCategory.Slug),
                    }),
            }))
            .ToListAsync(cancellationToken);

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
                    .Select(a =>
                    {
                        string? recordAgeCategory = a.IsRecord
                            ? RecordAgeCategorySelector.SelectBestLabel(a.RecordAgeCategorySlugs, r.Gender)
                            : null;

                        return new MeetAttempt(
                            a.Discipline,
                            a.Round,
                            a.Weight,
                            a.Good,
                            a.IsRecord,
                            recordAgeCategory);
                    });

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