using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Participations.ComputePlaces;

internal sealed class PlaceComputationService(ResultsDbContext dbContext)
{
    internal async Task ComputePlacesAsync(Participation participation, CancellationToken cancellationToken)
    {
        if (!participation.Meet.CalcPlaces)
        {
            return;
        }

        await RecomputeGroupAsync(
            participation.MeetId,
            participation.WeightCategoryId,
            participation.AgeCategoryId,
            calcPlaces: true,
            cancellationToken);
    }

    internal async Task RecomputeGroupAsync(
        int meetId,
        int weightCategoryId,
        int ageCategoryId,
        bool calcPlaces,
        CancellationToken cancellationToken)
    {
        if (!calcPlaces)
        {
            return;
        }

        List<Participation> groupParticipations = await dbContext.Set<Participation>()
            .Include(p => p.Attempts)
            .Where(p => p.MeetId == meetId)
            .Where(p => p.WeightCategoryId == weightCategoryId)
            .Where(p => p.AgeCategoryId == ageCategoryId)
            .ToListAsync(cancellationToken);

        RankGroup(groupParticipations);
    }

    private static void RankGroup(List<Participation> groupParticipations)
    {
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

        foreach (Participation p in groupParticipations.Where(p => p.Total == 0))
        {
            p.UpdateRanking(0);
        }
    }
}