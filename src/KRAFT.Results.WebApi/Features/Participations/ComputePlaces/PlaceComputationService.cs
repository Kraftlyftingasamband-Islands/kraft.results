using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Participations.ComputePlaces;

internal sealed class PlaceComputationService(ResultsDbContext dbContext)
{
    public async Task ComputePlacesAsync(Participation participation, CancellationToken cancellationToken)
    {
        if (!participation.Meet.CalcPlaces)
        {
            return;
        }

        List<Participation> groupParticipations = await dbContext.Set<Participation>()
            .Include(p => p.Attempts)
            .Where(p => p.MeetId == participation.MeetId)
            .Where(p => p.WeightCategoryId == participation.WeightCategoryId)
            .Where(p => p.AgeCategoryId == participation.AgeCategoryId)
            .ToListAsync(cancellationToken);

        List<Participation> ranked = groupParticipations
            .Where(p => p.Total > 0)
            .OrderByDescending(p => p.Total)
            .ThenBy(p => p.Weight.Value)
            .ThenBy(p => p.LotNo)
            .ToList();

        int rank = 1;

        foreach (Participation p in ranked)
        {
            p.UpdateRanking(rank);
            rank++;
        }

        foreach (Participation p in groupParticipations.Where(p => p.Total == 0 && p.Disqualified))
        {
            p.UpdateRanking(0);
        }
    }
}