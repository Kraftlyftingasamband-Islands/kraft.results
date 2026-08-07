using KRAFT.Results.Contracts.Meets;

namespace KRAFT.Results.WebApi.Features.Meets;

internal sealed record MeetProjection(
    string Slug,
    string Title,
    string? Location,
    DateOnly StartDate,
    MeetCategory Category,
    bool IsRaw,
    int ParticipantCount)
{
    internal MeetSummary ToMeetSummary(DateOnly today) =>

        // IsRaw (entity) maps to IsClassic (contract): raw powerlifting = equipment-free = classic style
        new(Slug, Title, Location, StartDate, Category.ToDisplayName(), IsRaw, StartDate > today, ParticipantCount);
}
