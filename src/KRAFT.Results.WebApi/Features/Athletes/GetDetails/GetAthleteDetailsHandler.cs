using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records;

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
        IReadOnlyList<AthleteRecord> records = await GetRecordsAsync(slug, cancellationToken);
        IReadOnlyList<AthleteParticipation> participations = await GetParticipationsAsync(slug, cancellationToken);
        IReadOnlyList<AthletePersonalBest> personalBests = await GetPersonalBestsAsync(slug, cancellationToken);

        AthleteDetails? athlete = await _dbContext.Set<Athlete>()
            .Where(x => x.Slug == slug)
            .Select(x => new AthleteDetails(
                x.Slug,
                $"{x.Firstname} {x.Lastname}",
                x.DateOfBirth != null && x.DateOfBirth.Value.Year > 0 ? x.DateOfBirth.Value.Year : null,
                x.Team != null ? x.Team.TitleFull : null,
                x.Team != null ? x.Team.Slug : null,
                0,
                personalBests,
                records,
                participations))
            .FirstOrDefaultAsync(cancellationToken);

        return athlete;
    }

    private static bool IsSquat(int disciplineId) => disciplineId == (byte)Discipline.Squat;

    private static bool IsBench(int disciplineId) => disciplineId == (byte)Discipline.Bench;

    private static bool IsDeadlift(int disciplineId) => disciplineId == (byte)Discipline.Deadlift;

    private static bool IsSingleLift(string meetType) => meetType != "Powerlifting";

#pragma warning disable S3358 // Ternary operators should not be nested
    private static string MapDiscipline(int disciplineId) =>
        IsSquat(disciplineId) ? Constants.Squat
        : IsBench(disciplineId) ? Constants.Bench
        : IsDeadlift(disciplineId) ? Constants.Deadlift
        : Constants.Total;

    private static string MapMeetType(string type) =>
        !IsSingleLift(type) ? Constants.Powerlifting
        : type == "Benchpress" ? $"{Constants.Bench} ({Constants.SingeLift})"
        : type == "Deadlift" ? $"{Constants.Deadlift} ({Constants.SingeLift})"
        : type;
#pragma warning restore S3358 // Ternary operators should not be nested

    private Task<List<AthleteRecord>> GetRecordsAsync(string slug, CancellationToken cancellationToken) =>
        _dbContext.Set<Record>()
        .Where(x => x.Attempt!.Participation.Athlete.Slug == slug)
        .Where(x => x.IsCurrent)
        .Where(x => x.Era.EndDate.Year > DateTime.UtcNow.Year)
        .OrderByDescending(x => x.Attempt!.Participation.Meet.StartDate)
        .ThenBy(x => x.WeightCategoryId)
        .ThenBy(x => x.AgeCategory.AgeCategoryId)
        .ThenBy(x => x.Attempt!.DisciplineId)
        .Select(x => new
        {
            x.Date,
            IsClassic = x.Attempt!.Participation.Meet.IsRaw,
            IsSingleLift = IsSingleLift(x.Attempt!.Participation.Meet.MeetType.Title),
            WeightCategory = x.WeightCategory.Title,
            AgeCategory = x.AgeCategory.Title,
            x.Attempt!.Participation.Total,
            x.Weight,
            x.Attempt!.DisciplineId,
            MeetTitle = x.Attempt!.Participation.Meet.Title,
            MeetYear = x.Attempt!.Participation.Meet.StartDate.Year,
            MeetSlug = x.Attempt!.Participation.Meet.Slug,
        })
        .Select(x => new AthleteRecord(
            x.Date,
            x.IsClassic,
            x.IsSingleLift,
            x.WeightCategory,
            x.AgeCategory,
            !x.IsSingleLift && x.Total == x.Weight ? Constants.Total : MapDiscipline(x.DisciplineId),
            x.Weight,
            $"{x.MeetTitle} {x.MeetYear}",
            x.MeetSlug))
        .ToListAsync(cancellationToken);

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
            p.Ipfpoints,
            p.Disqualified))
        .ToListAsync(cancellationToken);

    private async Task<List<AthletePersonalBest>> GetPersonalBestsAsync(string slug, CancellationToken cancellationToken)
    {
        List<PersonalBestRecord> bestLifts = await _dbContext.Set<Attempt>()
            .Where(x => x.Participation.Athlete.Slug == slug)
            .Where(x => !x.Participation.Disqualified)
            .Where(x => x.Good)
            .GroupBy(x => new
            {
                x.DisciplineId,
                x.Participation.Meet.IsRaw,
                x.Participation.Meet.MeetType.MeetTypeId,
            })
            .OrderBy(x => x.Key.MeetTypeId)
            .Select(x => x
                .OrderByDescending(a => a.Weight)
                .Select(a => new PersonalBestRecord(
                    a.Participation.Meet.IsRaw,
                    IsSingleLift(a.Participation.Meet.MeetType.Title),
                    a.DisciplineId,
                    a.Weight,
                    a.Participation.WeightCategory.Title,
                    a.Participation.Weight,
                    a.Participation.Meet.Title,
                    a.Participation.Meet.Slug,
                    a.Participation.Meet.MeetType.MeetTypeId,
                    a.Participation.Meet.StartDate))
                .First())
            .ToListAsync(cancellationToken);

        List<PersonalBestRecord> bestTotals = await _dbContext.Set<Participation>()
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
                .Select(p => new PersonalBestRecord(
                    p.Meet.IsRaw,
                    false,
                    0,
                    p.Total,
                    p.WeightCategory.Title,
                    p.Weight,
                    p.Meet.Title,
                    p.Meet.Slug,
                    p.Meet.MeetType.MeetTypeId,
                    p.Meet.StartDate))
                .First())
            .ToListAsync(cancellationToken);

        List<PersonalBestRecord> combined = [.. bestLifts, .. bestTotals];

        return [.. combined
            .OrderBy(x => x.MeetTypeId)
            .Select(x => new AthletePersonalBest(
            x.IsRaw,
            x.IsSingleLiftRecord,
            MapDiscipline(x.DisciplineId),
            x.Weight,
            x.WeightCategoryTitle,
            x.BodyWeight,
            x.MeetSlug,
            MapMeetType(x.MeetType),
            DateOnly.FromDateTime(x.MeetDate)))];
    }

    private sealed record class PersonalBestRecord(
        bool IsRaw,
        bool IsSingleLiftRecord,
        int DisciplineId,
        decimal Weight,
        string WeightCategoryTitle,
        decimal BodyWeight,
        string MeetSlug,
        string MeetType,
        int MeetTypeId,
        DateTime MeetDate);
}