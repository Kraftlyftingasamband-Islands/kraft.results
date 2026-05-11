namespace KRAFT.Results.WebApi.Features.Meets;

internal sealed record MeetProjection(
    string Slug,
    string Title,
    string? Location,
    DateOnly StartDate,
    MeetCategory Category,
    bool IsRaw,
    int ParticipantCount);