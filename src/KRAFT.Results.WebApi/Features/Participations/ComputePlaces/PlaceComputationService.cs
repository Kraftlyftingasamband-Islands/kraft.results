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

    internal async Task RecomputeMeetAsync(string slug, bool calcPlaces, CancellationToken cancellationToken)
    {
        List<Participation> participations = await dbContext.Set<Participation>()
            .Where(p => p.Meet.Slug == slug)
            .ToListAsync(cancellationToken);

        if (calcPlaces)
        {
            IEnumerable<IGrouping<(int WeightCategoryId, int AgeCategoryId), Participation>> groups = participations
                .GroupBy(p => (p.WeightCategoryId, p.AgeCategoryId));

            foreach (IGrouping<(int WeightCategoryId, int AgeCategoryId), Participation> group in groups)
            {
                RankGroup(group.ToList());
            }
        }
        else
        {
            foreach (Participation participation in participations)
            {
                participation.ClearRanking();
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
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
            .Where(p => !p.Disqualified)
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

        foreach (Participation p in groupParticipations.Where(p => p.Total == 0 || p.Disqualified))
        {
            p.UpdateRanking(0);
        }
    }
}