namespace KRAFT.Results.Contracts.Meets;

public sealed record class AddParticipantCommand(
    string AthleteSlug,
    decimal BodyWeight);