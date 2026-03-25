using KRAFT.Results.Contracts.Athletes;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Athletes.GetEditDetails;

internal sealed class GetAthleteEditDetailsHandler(ResultsDbContext dbContext)
{
    public Task<AthleteEditDetails?> Handle(string slug, CancellationToken cancellationToken) =>
        dbContext.Set<Athlete>()
            .Where(x => x.Slug == slug)
            .Select(x => new AthleteEditDetails(
                x.Firstname,
                x.Lastname,
                x.DateOfBirth,
                x.Gender.Value,
                x.CountryId,
                x.TeamId))
            .FirstOrDefaultAsync(cancellationToken);
}