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

    public Task<List<AthleteSummary>> Handle(CancellationToken cancellationToken) =>
        _dbContext.Set<Athlete>()
        .OrderBy(x => x.Firstname)
        .ThenBy(x => x.Lastname)
        .ThenBy(x => x.DateOfBirth)
        .Select(x => new AthleteSummary(
            x.Slug,
            $"{x.Firstname} {x.Lastname}",
            x.DateOfBirth != null && x.DateOfBirth.Value.Year > 1 ? x.DateOfBirth.Value.Year : null))
        .ToListAsync(cancellationToken);
}