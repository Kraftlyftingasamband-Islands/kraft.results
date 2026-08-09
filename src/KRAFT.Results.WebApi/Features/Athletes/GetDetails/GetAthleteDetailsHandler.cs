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
                x.Club != null ? x.Club.TitleFull : null,
                x.Club != null ? x.Club.Slug : null,
                x.Club != null ? x.Club.TitleShort : null,
                x.Club != null ? x.Club.LogoImageFilename : null,
                0,
                x.ProfileImageFilename))
            .FirstOrDefaultAsync(cancellationToken);
}
