using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.Features.AgeCategories;

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
        return meetDate.HasValue
            ? AgeCategory.ResolveEligibleSlugs(dateOfBirth, meetDate.Value)
            : ["open"];
    }

    private sealed record AthleteRow(string? Slug, string Name, int? YearOfBirth, DateOnly? DateOfBirth, string Gender);
}