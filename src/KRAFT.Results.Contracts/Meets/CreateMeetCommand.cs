using System.ComponentModel.DataAnnotations;

namespace KRAFT.Results.Contracts.Meets;

public sealed record class CreateMeetCommand(
    [MaxLength(100, ErrorMessage = "Nafn má ekki vera lengra en 100 stafir")]
    string Title,
    DateOnly StartDate,
    int? MeetTypeId,
    DateOnly? EndDate = null,
    bool CalcPlaces = true,
    string? Text = null,
    [MaxLength(50, ErrorMessage = "Staðsetning má ekki vera lengri en 50 stafir")]
    string? Location = null,
    bool PublishedResults = true,
    int ResultModeId = 1,
    bool PublishedInCalendar = true,
    bool IsInTeamCompetition = false,
    bool ShowWilks = true,
    bool ShowTeamPoints = true,
    bool ShowBodyWeight = true,
    bool ShowTeams = false,
    bool RecordsPossible = true,
    bool IsRaw = false);