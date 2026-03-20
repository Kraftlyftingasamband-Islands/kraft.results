using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Rankings;
using KRAFT.Results.WebApi.Features.Participations;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Rankings.Get;

internal sealed class GetRankingsHandler
{
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
            "total" => query.Where(p => p.Meet.MeetType.MeetTypeId == 1),
            "bench" => query.Where(p => new[] { 1, 2, 5 }.Contains(p.Meet.MeetType.MeetTypeId)),
            "squat" => query.Where(p => new[] { 1, 4 }.Contains(p.Meet.MeetType.MeetTypeId)),
            "deadlift" => query.Where(p => new[] { 1, 3 }.Contains(p.Meet.MeetType.MeetTypeId)),
            _ => query.Where(p => p.Meet.MeetType.MeetTypeId == 1),
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
                query = query.Where(p => p.Athlete.Gender == ValueObjects.Gender.Parse(genderLower));
            }
        }

        IQueryable<int> bestPerAthlete = query
            .GroupBy(p => p.AthleteId)
            .Select(g => g.OrderByDescending(p => p.Ipfpoints).Select(p => p.ParticipationId).First());

        query = query.Where(p => bestPerAthlete.Contains(p.ParticipationId));

        int totalCount = await query.CountAsync(cancellationToken);

        query = query.OrderByDescending(p => p.Ipfpoints);

        int skip = (page - 1) * pageSize;

        IQueryable<Participation> paged = query
            .Skip(skip)
            .Take(pageSize);

        IQueryable<RankingEntry> projection = disciplineKey switch
        {
            "squat" => paged.Select(p => new RankingEntry(0, p.Athlete.Firstname + " " + p.Athlete.Lastname, p.Athlete.Slug, p.Athlete.Gender.Value, p.Squat, p.WeightCategory.Title, p.Weight, p.Ipfpoints, p.Wilks, p.Meet.Title, p.Meet.Slug, DateOnly.FromDateTime(p.Meet.StartDate), p.Meet.IsRaw)),
            "bench" => paged.Select(p => new RankingEntry(0, p.Athlete.Firstname + " " + p.Athlete.Lastname, p.Athlete.Slug, p.Athlete.Gender.Value, p.Benchpress, p.WeightCategory.Title, p.Weight, p.Ipfpoints, p.Wilks, p.Meet.Title, p.Meet.Slug, DateOnly.FromDateTime(p.Meet.StartDate), p.Meet.IsRaw)),
            "deadlift" => paged.Select(p => new RankingEntry(0, p.Athlete.Firstname + " " + p.Athlete.Lastname, p.Athlete.Slug, p.Athlete.Gender.Value, p.Deadlift, p.WeightCategory.Title, p.Weight, p.Ipfpoints, p.Wilks, p.Meet.Title, p.Meet.Slug, DateOnly.FromDateTime(p.Meet.StartDate), p.Meet.IsRaw)),
            _ => paged.Select(p => new RankingEntry(0, p.Athlete.Firstname + " " + p.Athlete.Lastname, p.Athlete.Slug, p.Athlete.Gender.Value, p.Total, p.WeightCategory.Title, p.Weight, p.Ipfpoints, p.Wilks, p.Meet.Title, p.Meet.Slug, DateOnly.FromDateTime(p.Meet.StartDate), p.Meet.IsRaw)),
        };

        List<RankingEntry> items = await projection.ToListAsync(cancellationToken);

        for (int i = 0; i < items.Count; i++)
        {
            items[i] = items[i] with { Rank = skip + i + 1 };
        }

        return new PagedResponse<RankingEntry>(items, page, pageSize, totalCount);
    }
}