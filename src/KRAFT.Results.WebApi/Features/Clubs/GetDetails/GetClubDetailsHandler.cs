using KRAFT.Results.Contracts.Clubs;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Clubs.GetDetails;

internal sealed class GetClubDetailsHandler
{
    private readonly ResultsDbContext _dbContext;

    public GetClubDetailsHandler(ResultsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ClubDetails?> Handle(string slug, CancellationToken cancellationToken) =>
        _dbContext.Set<Club>()
        .Where(x => x.Slug == slug)
        .Select(x => new ClubDetails(
            x.Slug,
            x.Title,
            x.TitleShort,
            x.TitleFull,
            x.Country.Value,
            x.LogoImageFilename,
            x.Athletes
                .OrderBy(x => x.Firstname)
                .ThenBy(x => x.Lastname)
                .ThenBy(x => x.DateOfBirth)
                .Select(a => new ClubMember(
                    a.Slug,
                    $"{a.Firstname} {a.Lastname}",
                    a.DateOfBirth != null && a.DateOfBirth.Value.Year > 1 ? a.DateOfBirth.Value.Year : null,
                    a.Participations.Count,
                    a.ProfileImageFilename))
            .ToList()))
        .FirstOrDefaultAsync(cancellationToken);
}
