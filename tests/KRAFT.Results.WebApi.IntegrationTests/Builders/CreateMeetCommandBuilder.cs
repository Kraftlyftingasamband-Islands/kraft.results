using KRAFT.Results.Contracts.Meets;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class CreateMeetCommandBuilder
{
    private string _title = Guid.NewGuid().ToString();
    private DateOnly _startDate = DateOnly.FromDateTime(DateTime.UtcNow);
    private int? _meetTypeId;
    private DateOnly? _endDate;
    private bool _calcPlaces = true;
    private string? _text;
    private string? _location;
    private bool _publishedResults = true;
    private int _resultModeId = 1;
    private bool _publishedInCalendar = true;
    private bool _isInTeamCompetition;
    private bool _showWilks = true;
    private bool _showTeamPoints = true;
    private bool _showBodyWeight = true;
    private bool _showTeams;
    private bool _recordsPossible = true;
    private bool _isRaw;

    public CreateMeetCommandBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public CreateMeetCommandBuilder WithStartDate(DateOnly startDate)
    {
        _startDate = startDate;
        return this;
    }

    public CreateMeetCommandBuilder WithMeetTypeId(int? meetTypeId)
    {
        _meetTypeId = meetTypeId;
        return this;
    }

    public CreateMeetCommandBuilder WithEndDate(DateOnly? endDate)
    {
        _endDate = endDate;
        return this;
    }

    public CreateMeetCommandBuilder WithCalcPlaces(bool calcPlaces)
    {
        _calcPlaces = calcPlaces;
        return this;
    }

    public CreateMeetCommandBuilder WithText(string? text)
    {
        _text = text;
        return this;
    }

    public CreateMeetCommandBuilder WithLocation(string? location)
    {
        _location = location;
        return this;
    }

    public CreateMeetCommandBuilder WithPublishedResults(bool publishedResults)
    {
        _publishedResults = publishedResults;
        return this;
    }

    public CreateMeetCommandBuilder WithResultModeId(int resultModeId)
    {
        _resultModeId = resultModeId;
        return this;
    }

    public CreateMeetCommandBuilder WithPublishedInCalendar(bool publishedInCalendar)
    {
        _publishedInCalendar = publishedInCalendar;
        return this;
    }

    public CreateMeetCommandBuilder WithIsInTeamCompetition(bool isInTeamCompetition)
    {
        _isInTeamCompetition = isInTeamCompetition;
        return this;
    }

    public CreateMeetCommandBuilder WithShowWilks(bool showWilks)
    {
        _showWilks = showWilks;
        return this;
    }

    public CreateMeetCommandBuilder WithShowTeamPoints(bool showTeamPoints)
    {
        _showTeamPoints = showTeamPoints;
        return this;
    }

    public CreateMeetCommandBuilder WithShowBodyWeight(bool showBodyWeight)
    {
        _showBodyWeight = showBodyWeight;
        return this;
    }

    public CreateMeetCommandBuilder WithShowTeams(bool showTeams)
    {
        _showTeams = showTeams;
        return this;
    }

    public CreateMeetCommandBuilder WithRecordsPossible(bool recordsPossible)
    {
        _recordsPossible = recordsPossible;
        return this;
    }

    public CreateMeetCommandBuilder WithIsRaw(bool isRaw)
    {
        _isRaw = isRaw;
        return this;
    }

    public CreateMeetCommand Build() =>
        new(
            _title,
            _startDate,
            _meetTypeId,
            _endDate,
            _calcPlaces,
            _text,
            _location,
            _publishedResults,
            _resultModeId,
            _publishedInCalendar,
            _isInTeamCompetition,
            _showWilks,
            _showTeamPoints,
            _showBodyWeight,
            _showTeams,
            _recordsPossible,
            _isRaw);
}