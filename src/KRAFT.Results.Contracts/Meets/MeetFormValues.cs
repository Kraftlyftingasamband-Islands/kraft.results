namespace KRAFT.Results.Contracts.Meets;

public sealed record MeetFormValues(
    string Title,
    DateOnly StartDate,
    int MeetTypeId,
    DateOnly? EndDate,
    string? Location,
    string? Text,
    bool CalcPlaces,
    int ResultModeId,
    bool PublishedResults,
    bool PublishedInCalendar,
    bool IsInTeamCompetition,
    bool ShowWilks,
    bool ShowTeamPoints,
    bool ShowBodyWeight,
    bool ShowTeams,
    bool RecordsPossible,
    bool IsRaw);