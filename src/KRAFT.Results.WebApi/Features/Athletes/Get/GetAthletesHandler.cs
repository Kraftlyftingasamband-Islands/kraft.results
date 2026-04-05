using KRAFT.Results.Contracts.Athletes;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Athletes.Get;

internal sealed class GetAthletesHandler
{
    private readonly ResultsDbContext _dbContext;

    public GetAthletesHandler(ResultsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AthleteSummary>> Handle(string? search, DateOnly? meetDate, CancellationToken cancellationToken)
    {
        IQueryable<Athlete> query = _dbContext.Set<Athlete>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => (a.Firstname + " " + a.Lastname).Contains(search));
        }

        List<AthleteRow> rows = await query
            .OrderBy(x => x.Firstname)
            .ThenBy(x => x.Lastname)
            .ThenBy(x => x.DateOfBirth)
            .Select(x => new AthleteRow(
                x.Slug,
                x.Firstname + " " + x.Lastname,
                x.DateOfBirth != null && x.DateOfBirth.Value.Year > 1 ? (int?)x.DateOfBirth.Value.Year : null,
                x.DateOfBirth != null && x.DateOfBirth.Value.Year > 1 ? x.DateOfBirth : null,
                x.Gender == "f" ? "f" : "m"))
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new AthleteSummary(
                x.Slug,
                x.Name,
                x.YearOfBirth,
                x.Gender,
                ResolveEligibleSlugs(x.DateOfBirth, meetDate)))
            .ToList();
    }

    private static IReadOnlyList<string> ResolveEligibleSlugs(DateOnly? dateOfBirth, DateOnly? meetDate)
    {
        string primary = ResolveSlug(dateOfBirth, meetDate);
        return primary switch
        {
            "subjunior" => ["subjunior", "junior", "open"],
            "junior" => ["junior", "open"],
            "masters1" or "masters2" or "masters3" or "masters4" => [primary, "open"],
            _ => ["open"],
        };
    }

    private static string ResolveSlug(DateOnly? dateOfBirth, DateOnly? meetDate)
    {
        if (dateOfBirth is null || meetDate is null)
        {
            return "open";
        }

        int age = meetDate.Value.Year - dateOfBirth.Value.Year;

        if (dateOfBirth.Value.Month > meetDate.Value.Month ||
            (dateOfBirth.Value.Month == meetDate.Value.Month && dateOfBirth.Value.Day > meetDate.Value.Day))
        {
            age--;
        }

        return age switch
        {
            <= 18 => "subjunior",
            <= 23 => "junior",
            >= 40 and <= 49 => "masters1",
            >= 50 and <= 59 => "masters2",
            >= 60 and <= 69 => "masters3",
            >= 70 => "masters4",
            _ => "open",
        };
    }

    private sealed record AthleteRow(string? Slug, string Name, int? YearOfBirth, DateOnly? DateOfBirth, string Gender);
}