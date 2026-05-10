using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Photos;
using KRAFT.Results.WebApi.Features.Users;

namespace KRAFT.Results.WebApi.Features.Meets;

internal sealed class Meet : AggregateRoot
{
    internal const int TitleMaxLength = 100;
    internal const int LocationMaxLength = 50;
    internal const int TextMaxLength = 8000;
    internal const int StartDateMinimumYear = 1900;

    // For EF core
    private Meet()
    {
    }

    public string Title { get; private set; } = null!;

    public string Slug { get; private set; } = null!;

    public DateTime StartDate { get; private set; }

    public DateTime EndDate { get; private set; }

    public bool CalcPlaces { get; private set; }

    public string? Text { get; private set; }

    public string? Location { get; private set; }

    public bool PublishedResults { get; private set; }

    public int ResultModeId { get; private set; }

    public bool PublishedInCalendar { get; private set; }

    public bool IsInTeamCompetition { get; private set; }

    public bool ShowWilks { get; private set; }

    public bool ShowTeamPoints { get; private set; }

    public bool ShowBodyWeight { get; private set; }

    public bool ShowTeams { get; private set; }

    public bool RecordsPossible { get; private set; }

    public bool IsRaw { get; private set; }

    public DateTime CreatedOn { get; private set; }

    public DateTime ModifiedOn { get; private set; }

    public string ModifiedBy { get; private set; } = null!;

    public string CreatedBy { get; private set; } = null!;

    public MeetCategory Category { get; private set; }

    public ICollection<Participation> Participations { get; } = [];

    public ICollection<Photo> Photos { get; } = [];

    internal static Result<Meet> Create(
        User creator,
        MeetCategory category,
        string title,
        DateOnly startDate,
        DateOnly? endDate = null,
        bool calcPlaces = true,
        string? text = null,
        string? location = null,
        bool publishedResults = true,
        int resultModeId = 1,
        bool publishedInCalendar = true,
        bool isInTeamCompetition = false,
        bool showWilks = true,
        bool showTeamPoints = true,
        bool showBodyWeight = true,
        bool showTeams = false,
        bool recordsPossible = true,
        bool isRaw = false)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return MeetErrors.EmptyTitle;
        }

        if (title.Length > TitleMaxLength)
        {
            return MeetErrors.TitleTooLong;
        }

        if (startDate.Year < StartDateMinimumYear)
        {
            return MeetErrors.InvalidStartDate(startDate);
        }

        DateOnly resolvedEndDate = endDate ?? startDate;

        if (resolvedEndDate < startDate)
        {
            return MeetErrors.EndDateBeforeStartDate;
        }

        if (!IsValidResultMode(resultModeId))
        {
            return MeetErrors.InvalidResultMode;
        }

        string? normalizedLocation = NormalizeOptionalString(location);

        if (normalizedLocation is not null && normalizedLocation.Length > LocationMaxLength)
        {
            return MeetErrors.LocationTooLong;
        }

        string? normalizedText = NormalizeOptionalString(text);

        if (normalizedText is not null && normalizedText.Length > TextMaxLength)
        {
            return MeetErrors.TextTooLong;
        }

        DateTime startDateTime = startDate.ToDateTime(TimeOnly.MinValue);

        Meet meet = new()
        {
            Category = category,
            Title = title,
            StartDate = startDateTime,
            EndDate = resolvedEndDate.ToDateTime(TimeOnly.MinValue),
            CalcPlaces = calcPlaces,
            Text = normalizedText,
            Location = normalizedLocation,
            PublishedResults = publishedResults,
            ResultModeId = resultModeId,
            PublishedInCalendar = publishedInCalendar,
            IsInTeamCompetition = isInTeamCompetition,
            ShowWilks = showWilks,
            ShowTeamPoints = showTeamPoints,
            ShowBodyWeight = showBodyWeight,
            ShowTeams = showTeams,
            RecordsPossible = recordsPossible,
            IsRaw = isRaw,
            Slug = ValueObjects.Slug.Create($"{title} {startDate.Year}"),
            CreatedOn = DateTime.UtcNow,
            CreatedBy = creator.Username,
        };

        return meet;
    }

    internal Result Update(
        User modifier,
        MeetCategory category,
        string title,
        DateOnly startDate,
        DateOnly? endDate = null,
        bool calcPlaces = true,
        string? text = null,
        string? location = null,
        bool publishedResults = true,
        int resultModeId = 1,
        bool publishedInCalendar = true,
        bool isInTeamCompetition = false,
        bool showWilks = true,
        bool showTeamPoints = true,
        bool showBodyWeight = true,
        bool showTeams = false,
        bool recordsPossible = true,
        bool isRaw = false)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return MeetErrors.EmptyTitle;
        }

        if (title.Length > TitleMaxLength)
        {
            return MeetErrors.TitleTooLong;
        }

        if (startDate.Year < StartDateMinimumYear)
        {
            return MeetErrors.InvalidStartDate(startDate);
        }

        DateOnly resolvedEndDate = endDate ?? startDate;

        if (resolvedEndDate < startDate)
        {
            return MeetErrors.EndDateBeforeStartDate;
        }

        if (!IsValidResultMode(resultModeId))
        {
            return MeetErrors.InvalidResultMode;
        }

        string? normalizedLocation = NormalizeOptionalString(location);

        if (normalizedLocation is not null && normalizedLocation.Length > LocationMaxLength)
        {
            return MeetErrors.LocationTooLong;
        }

        string? normalizedText = NormalizeOptionalString(text);

        if (normalizedText is not null && normalizedText.Length > TextMaxLength)
        {
            return MeetErrors.TextTooLong;
        }

        bool calcPlacesChanged = CalcPlaces != calcPlaces;

        DateTime startDateTime = startDate.ToDateTime(TimeOnly.MinValue);

        Title = title;
        StartDate = startDateTime;
        EndDate = resolvedEndDate.ToDateTime(TimeOnly.MinValue);
        Category = category;
        CalcPlaces = calcPlaces;
        Text = normalizedText;
        Location = normalizedLocation;
        PublishedResults = publishedResults;
        ResultModeId = resultModeId;
        PublishedInCalendar = publishedInCalendar;
        IsInTeamCompetition = isInTeamCompetition;
        ShowWilks = showWilks;
        ShowTeamPoints = showTeamPoints;
        ShowBodyWeight = showBodyWeight;
        ShowTeams = showTeams;
        RecordsPossible = recordsPossible;
        IsRaw = isRaw;
        ModifiedOn = DateTime.UtcNow;
        ModifiedBy = modifier.Username;

        if (calcPlacesChanged)
        {
            Raise(new CalcPlacesChangedEvent(Slug, calcPlaces));
        }

        return Result.Success();
    }

    private static bool IsValidResultMode(int resultModeId) =>
        resultModeId != (int)ResultMode.None && Enum.IsDefined(typeof(ResultMode), resultModeId);

    private static string? NormalizeOptionalString(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}