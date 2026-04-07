using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.AgeCategories;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Teams;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Features.WeightCategories;
using KRAFT.Results.WebApi.ValueObjects;

namespace KRAFT.Results.WebApi.Features.Participations;

internal sealed class Participation : AggregateRoot
{
    // For EF Core
    private Participation()
    {
    }

    public int ParticipationId { get; private set; }

    public int AthleteId { get; private set; }

    public int MeetId { get; private set; }

    public BodyWeight Weight { get; private set; } = null!;

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

    internal static Result<Participation> Create(User creator, int athleteId, int meetId, int weightCategoryId, int ageCategoryId, decimal bodyWeight, int? teamId = null)
    {
        if (athleteId <= 0)
        {
            return ParticipationErrors.AthleteIdMustBePositive;
        }

        if (meetId <= 0)
        {
            return ParticipationErrors.MeetIdMustBePositive;
        }

        if (weightCategoryId <= 0)
        {
            return ParticipationErrors.WeightCategoryIdMustBePositive;
        }

        if (ageCategoryId <= 0)
        {
            return ParticipationErrors.AgeCategoryIdMustBePositive;
        }

        Result<BodyWeight> bodyWeightResult = BodyWeight.Create(bodyWeight);

        if (bodyWeightResult.IsFailure)
        {
            return bodyWeightResult.Error;
        }

        Participation participation = new()
        {
            AthleteId = athleteId,
            MeetId = meetId,
            WeightCategoryId = weightCategoryId,
            AgeCategoryId = ageCategoryId,
            Weight = bodyWeightResult.FromResult(),
            TeamId = teamId,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = creator.Username,
        };

        participation.Raise(new ParticipationAddedEvent(participation));

        return participation;
    }

    internal void UpdateAgeCategory(int ageCategoryId, string modifiedBy)
    {
        AgeCategoryId = ageCategoryId;
        ModifiedOn = DateTime.UtcNow;
        ModifiedBy = modifiedBy;
    }

    internal Result UpdateBodyWeight(decimal bodyWeight, string modifiedBy)
    {
        Result<BodyWeight> bodyWeightResult = BodyWeight.Create(bodyWeight);

        if (bodyWeightResult.IsFailure)
        {
            return Result.Failure(bodyWeightResult.Error);
        }

        Weight = bodyWeightResult.FromResult();
        ModifiedOn = DateTime.UtcNow;
        ModifiedBy = modifiedBy;

        return Result.Success();
    }

    internal void RecordAttempt(Discipline discipline, short round, decimal weight, bool good, string createdBy)
    {
        Attempt attempt = Attempt.Create(ParticipationId, discipline, round, weight, good, createdBy);
        Attempts.Add(attempt);
        Raise(new AttemptRecordedEvent(this));
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
            if (attempt.Discipline == discipline && attempt.Good && attempt.Weight > best)
            {
                best = attempt.Weight;
            }
        }

        return best;
    }
}