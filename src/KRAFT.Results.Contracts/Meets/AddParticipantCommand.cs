namespace KRAFT.Results.Contracts.Meets;

public sealed record class AddParticipantCommand(
    int AthleteId,
    int WeightCategoryId,
    decimal? BodyWeight);