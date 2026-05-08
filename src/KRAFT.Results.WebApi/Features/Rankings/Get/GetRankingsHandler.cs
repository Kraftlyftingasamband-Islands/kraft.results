using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Rankings;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Rankings.Get;

internal sealed class GetRankingsHandler
{
    private static readonly MeetCategory[] BenchMeetCategories =
        [MeetCategory.Powerlifting, MeetCategory.Benchpress, MeetCategory.PushPull];

    private static readonly MeetCategory[] SquatMeetCategories =
        [MeetCategory.Powerlifting];

    private static readonly MeetCategory[] DeadliftMeetCategories =
        [MeetCategory.Powerlifting, MeetCategory.Deadlift];

    private readonly ResultsDbContext _dbContext;

    public GetRankingsHandler(ResultsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<RankingEntry>> Handle(
        string discipline,
        int? year,
        string? equipmentType,
        string? gender,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        string disciplineKey = discipline.ToLowerInvariant();

        IQueryable<Participation> query = _dbContext.Set<Participation>()
            .Where(p => !p.Disqualified)
            .Where(p => p.Athlete.Country.Iso2 == "IS");

        query = disciplineKey switch
        {
            "squat" => query.Where(p => p.Squat > 0),
            "bench" => query.Where(p => p.Benchpress > 0),
            "deadlift" => query.Where(p => p.Deadlift > 0),
            _ => query.Where(p => p.Total > 0),
        };

        query = disciplineKey switch
        {
            "total" => query.Where(p => p.Meet.Category == MeetCategory.Powerlifting),
            "bench" => query.Where(p => BenchMeetCategories.Contains(p.Meet.Category)),
            "squat" => query.Where(p => SquatMeetCategories.Contains(p.Meet.Category)),
            "deadlift" => query.Where(p => DeadliftMeetCategories.Contains(p.Meet.Category)),
            _ => query.Where(p => p.Meet.Category == MeetCategory.Powerlifting),
        };

        if (year is not null && year > 0)
        {
            query = query.Where(p => p.Meet.StartDate.Year == year);
        }

        if (equipmentType is not null)
        {
            query = equipmentType.ToLowerInvariant() switch
            {
                "classic" => query.Where(p => p.Meet.IsRaw),
                "equipped" => query.Where(p => !p.Meet.IsRaw),
                _ => query,
            };
        }

        if (gender is not null)
        {
            string genderLower = gender.ToLowerInvariant();
            if (genderLower is "m" or "f")
            {
                query = query.Where(p => p.Athlete.Gender == Gender.Parse(genderLower));
            }
        }

        IQueryable<RawRankingData> rawQuery = disciplineKey switch
        {
            "squat" => query.Select(p => new RawRankingData(
                p.AthleteId,
                p.Athlete.Firstname + " " + p.Athlete.Lastname,
                p.Athlete.Slug,
                p.Athlete.Gender.Value,
                p.Squat,
                p.WeightCategory.Title,
                p.Weight.Value,
                p.Wilks,
                p.Meet.Slug,
                p.Meet.IsRaw,
                DateOnly.FromDateTime(p.Meet.StartDate))),
            "bench" => query.Select(p => new RawRankingData(
                p.AthleteId,
                p.Athlete.Firstname + " " + p.Athlete.Lastname,
                p.Athlete.Slug,
                p.Athlete.Gender.Value,
                p.Benchpress,
                p.WeightCategory.Title,
                p.Weight.Value,
                p.Wilks,
                p.Meet.Slug,
                p.Meet.IsRaw,
                DateOnly.FromDateTime(p.Meet.StartDate))),
            "deadlift" => query.Select(p => new RawRankingData(
                p.AthleteId,
                p.Athlete.Firstname + " " + p.Athlete.Lastname,
                p.Athlete.Slug,
                p.Athlete.Gender.Value,
                p.Deadlift,
                p.WeightCategory.Title,
                p.Weight.Value,
                p.Wilks,
                p.Meet.Slug,
                p.Meet.IsRaw,
                DateOnly.FromDateTime(p.Meet.StartDate))),
            _ => query.Select(p => new RawRankingData(
                p.AthleteId,
                p.Athlete.Firstname + " " + p.Athlete.Lastname,
                p.Athlete.Slug,
                p.Athlete.Gender.Value,
                p.Total,
                p.WeightCategory.Title,
                p.Weight.Value,
                p.Wilks,
                p.Meet.Slug,
                p.Meet.IsRaw,
                DateOnly.FromDateTime(p.Meet.StartDate))),
        };

        List<RawRankingData> rawData = await rawQuery.ToListAsync(cancellationToken);

        MeetCategory ipfCategory = disciplineKey == "bench"
            ? MeetCategory.Benchpress
            : MeetCategory.Powerlifting;

        foreach (RawRankingData row in rawData)
        {
            Gender parsedGender = Gender.Parse(row.Gender);
            IpfPoints ipfPoints = IpfPoints.Create(row.IsRaw, parsedGender, ipfCategory, row.BodyWeight, row.Result);
            row.CalculatedIpfPoints = ipfPoints.Value;
        }

        List<RawRankingData> bestPerAthlete = rawData
            .GroupBy(r => r.AthleteId)
            .Select(g => g.OrderByDescending(r => r.CalculatedIpfPoints).First())
            .OrderByDescending(r => r.CalculatedIpfPoints)
            .ToList();

        int totalCount = bestPerAthlete.Count;
        int skip = (page - 1) * pageSize;

        List<RankingEntry> items = bestPerAthlete
            .Skip(skip)
            .Take(pageSize)
            .Select((r, i) => new RankingEntry(
                skip + i + 1,
                r.AthleteName,
                r.AthleteSlug,
                r.Gender,
                r.Result,
                r.WeightCategory,
                r.BodyWeight,
                r.CalculatedIpfPoints,
                r.Wilks,
                r.MeetSlug,
                r.IsRaw,
                r.MeetDate))
            .ToList();

        return new PagedResponse<RankingEntry>(items, page, pageSize, totalCount);
    }

    private sealed record RawRankingData(
        int AthleteId,
        string AthleteName,
        string AthleteSlug,
        string Gender,
        decimal Result,
        string WeightCategory,
        decimal BodyWeight,
        decimal Wilks,
        string MeetSlug,
        bool IsRaw,
        DateOnly MeetDate)
    {
        public decimal CalculatedIpfPoints { get; set; }
    }
}