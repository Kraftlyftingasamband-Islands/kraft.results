using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Countries;

internal static class CountryDbSetExtensions
{
    internal static Task<Country?> GetCountryAsync(this DbContext dbContext, int id, CancellationToken cancellationToken) =>
        dbContext.Set<Country>()
        .Where(x => x.CountryId == id)
        .FirstOrDefaultAsync(cancellationToken);
}