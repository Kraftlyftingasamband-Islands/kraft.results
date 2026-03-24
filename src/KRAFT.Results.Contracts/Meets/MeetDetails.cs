namespace KRAFT.Results.Contracts.Meets;

public sealed record class MeetDetails(
    string Title,
    string Slug,
    string Location,
    string Text,
    DateOnly StartDate,
    DateOnly? EndDate,
    string Type,
    int MeetTypeId,
    string ResultMode,
    bool CalculatePlaces,
    bool IsInTeamCompetition,
    bool ShowWilks,
    bool ShowTeams,
    bool ShowBodyWeight,
    bool PublishedInCalendar,
    bool PublishedResults,
    bool RecordsPossible,
    bool IsClassic);