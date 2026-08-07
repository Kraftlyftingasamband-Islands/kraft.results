namespace KRAFT.Results.Contracts.Meets;

public sealed record class MeetSummary(
    string Slug,
    string Title,
    string? Location,
    DateOnly StartDate,
    string Category,
    bool IsClassic,
    bool IsUpcoming,
    int ParticipantCount);
