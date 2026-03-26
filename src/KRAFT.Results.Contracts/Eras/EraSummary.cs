namespace KRAFT.Results.Contracts.Eras;

public sealed record class EraSummary(
    int EraId,
    string Title,
    string Slug,
    DateOnly StartDate,
    DateOnly EndDate);