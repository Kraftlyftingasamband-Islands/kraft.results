using KRAFT.Results.Contracts.Eras;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Eras.Get;

internal sealed class GetErasHandler(ResultsDbContext dbContext)
{
    public async Task<List<EraSummary>> Handle(CancellationToken cancellationToken)
    {
        List<EraSummary> eras = await dbContext.Set<Era>()
            .Where(e => e.Slug != null)
            .OrderBy(e => e.StartDate)
            .Select(e => new EraSummary(
                e.EraId,
                e.Title,
                e.Slug!,
                e.StartDate,
                e.EndDate))
            .ToListAsync(cancellationToken);

        return eras;
    }
}