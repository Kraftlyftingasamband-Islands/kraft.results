using KRAFT.Results.Contracts.Eras;
using KRAFT.Results.WebApi.Features.Records;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Eras.Get;

internal sealed class GetErasHandler(ResultsDbContext dbContext)
{
    public async Task<List<EraSummary>> Handle(CancellationToken cancellationToken)
    {
        List<EraSummary> eras = await dbContext.Set<Era>()
            .Where(e => e.Slug != null)
            .Where(e => dbContext.Set<Record>().Any(r => r.EraId == e.EraId && r.IsCurrent))
            .OrderBy(e => e.StartDate)
            .Select(e => new EraSummary(
                e.EraId,
                e.Title,
                e.Slug!,
                e.StartDate,
                e.EndDate,
                dbContext.Set<Record>().Any(r => r.EraId == e.EraId && r.IsCurrent && r.IsRaw),
                dbContext.Set<Record>().Any(r => r.EraId == e.EraId && r.IsCurrent && !r.IsRaw)))
            .ToListAsync(cancellationToken);

        return eras;
    }
}