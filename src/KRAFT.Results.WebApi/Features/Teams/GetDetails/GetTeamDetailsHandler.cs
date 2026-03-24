using KRAFT.Results.Contracts.Teams;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Teams.GetDetails;

internal sealed class GetTeamDetailsHandler
{
    private readonly ResultsDbContext _dbContext;

    public GetTeamDetailsHandler(ResultsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<TeamDetails?> Handle(string slug, CancellationToken cancellationToken) =>
        _dbContext.Set<Team>()
        .Where(x => x.Slug == slug)
        .Select(x => new TeamDetails(
            x.Slug,
            x.Title,
            x.TitleShort,
            x.TitleFull,
            x.CountryId,
            x.Athletes
                .OrderBy(x => x.Firstname)
                .ThenBy(x => x.Lastname)
                .ThenBy(x => x.DateOfBirth)
                .Select(a => new TeamMember(
                    a.Slug,
                    $"{a.Firstname} {a.Lastname}",
                    a.DateOfBirth != null && a.DateOfBirth.Value.Year > 1 ? a.DateOfBirth.Value.Year : null))
            .ToList()))
        .FirstOrDefaultAsync(cancellationToken);
}