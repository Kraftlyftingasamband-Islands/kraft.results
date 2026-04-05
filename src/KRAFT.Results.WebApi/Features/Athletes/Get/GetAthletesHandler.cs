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

    public Task<List<AthleteSummary>> Handle(string? search, CancellationToken cancellationToken)
    {
        IQueryable<Athlete> query = _dbContext.Set<Athlete>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => (a.Firstname + " " + a.Lastname).Contains(search));
        }

        return query
            .OrderBy(x => x.Firstname)
            .ThenBy(x => x.Lastname)
            .ThenBy(x => x.DateOfBirth)
            .Select(x => new AthleteSummary(
                x.Slug,
                $"{x.Firstname} {x.Lastname}",
                x.DateOfBirth != null && x.DateOfBirth.Value.Year > 1 ? x.DateOfBirth.Value.Year : null,
                x.DateOfBirth != null && x.DateOfBirth.Value.Year > 1 ? x.DateOfBirth : null,
                x.Gender == "f" ? "f" : "m"))
            .ToListAsync(cancellationToken);
    }
}