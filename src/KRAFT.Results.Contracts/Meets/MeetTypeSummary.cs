namespace KRAFT.Results.Contracts.Meets;

public sealed record class MeetTypeSummary(int Id, string Title, string DisplayName, IReadOnlyList<string> Disciplines);