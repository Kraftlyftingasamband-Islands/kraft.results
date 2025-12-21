using KRAFT.Results.Contracts.Athletes;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Athletes.GetDetails;

internal sealed class GetAthleteDetailsHandler(ResultsDbContext dbContext)
{
    public Task<AthleteDetails?> Handle(string slug, CancellationToken cancellationToken) =>
        dbContext.Set<Athlete>()
            .Where(x => x.Slug == slug)
            .Select(x => new AthleteDetails(
                x.Slug,
                $"{x.Firstname} {x.Lastname}",
                x.DateOfBirth != null && x.DateOfBirth.Value.Year > 0 ? x.DateOfBirth.Value.Year : null,
                x.Team != null ? x.Team.TitleFull : null,
                x.Team != null ? x.Team.Slug : null,
                0))
            .FirstOrDefaultAsync(cancellationToken);
}