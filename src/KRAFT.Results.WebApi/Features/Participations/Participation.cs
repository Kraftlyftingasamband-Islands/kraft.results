using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Features.AgeCategories;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Teams;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Features.WeightCategories;

namespace KRAFT.Results.WebApi.Features.Participations;

internal sealed class Participation
{
    // For EF Core
    private Participation()
    {
    }

    public int ParticipationId { get; private set; }

    public int AthleteId { get; private set; }

    public int MeetId { get; private set; }

    public decimal Weight { get; private set; }

    public int WeightCategoryId { get; private set; }

    public int? TeamId { get; private set; }

    public int AgeCategoryId { get; private set; }

    public int Place { get; private set; }

    public bool Disqualified { get; private set; }

    public decimal Squat { get; private set; }

    public decimal Benchpress { get; private set; }

    public decimal Deadlift { get; private set; }

    public decimal Total { get; private set; }

    public decimal Wilks { get; private set; }

    public decimal? Ipfpoints { get; private set; }

    public DateTime CreatedOn { get; private set; }

    public DateTime ModifiedOn { get; private set; }

    public string ModifiedBy { get; private set; } = null!;

    public string CreatedBy { get; private set; } = null!;

    public int LotNo { get; private set; }

    public int? TeamPoints { get; private set; }

    public AgeCategory AgeCategory { get; private set; } = null!;

    public Athlete Athlete { get; private set; } = null!;

    public ICollection<Attempt> Attempts { get; } = [];

    public Meet Meet { get; private set; } = null!;

    public Team? Team { get; private set; }

    public WeightCategory WeightCategory { get; private set; } = null!;

    internal static Participation Create(User creator, int athleteId, int meetId, int weightCategoryId, decimal bodyWeight)
    {
        return new Participation
        {
            AthleteId = athleteId,
            MeetId = meetId,
            WeightCategoryId = weightCategoryId,
            Weight = bodyWeight,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = creator.Username,
        };
    }

    internal void RecalculateTotals()
    {
        decimal bestSquat = BestGoodLift(Discipline.Squat);
        decimal bestBench = BestGoodLift(Discipline.Bench);
        decimal bestDeadlift = BestGoodLift(Discipline.Deadlift);

        Squat = bestSquat;
        Benchpress = bestBench;
        Deadlift = bestDeadlift;

        bool bombedOut = bestSquat == 0 || bestBench == 0 || bestDeadlift == 0;
        Total = bombedOut ? 0 : bestSquat + bestBench + bestDeadlift;
    }

    private decimal BestGoodLift(Discipline discipline)
    {
        decimal best = 0;

        foreach (Attempt attempt in Attempts)
        {
            if (attempt.DisciplineId == (byte)discipline && attempt.Good && attempt.Weight > best)
            {
                best = attempt.Weight;
            }
        }

        return best;
    }
}