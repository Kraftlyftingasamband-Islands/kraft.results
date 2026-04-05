using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Photos;
using KRAFT.Results.WebApi.Features.Users;

namespace KRAFT.Results.WebApi.Features.Meets;

internal sealed class Meet
{
    internal const int TitleMaxLength = 100;
    internal const int LocationMaxLength = 50;
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

    public MeetType MeetType { get; private set; } = null!;

    public ICollection<Participation> Participations { get; } = [];

    public ICollection<Photo> Photos { get; } = [];

    internal static Result<Meet> Create(User creator, MeetType type, string title, DateOnly startDate)
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

        DateTime date = startDate.ToDateTime(TimeOnly.MinValue);

        Meet meet = new()
        {
            MeetType = type,
            Title = title,
            StartDate = date,
            EndDate = date,
            Slug = ValueObjects.Slug.Create($"{title} {startDate.Year}"),
            CreatedOn = DateTime.UtcNow,
            CreatedBy = creator.Username,
        };

        return meet;
    }

    internal Result Update(User modifier, MeetType type, string title, DateOnly startDate)
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

        DateTime date = startDate.ToDateTime(TimeOnly.MinValue);

        Title = title;
        StartDate = date;
        EndDate = date;
        MeetType = type;
        ModifiedOn = DateTime.UtcNow;
        ModifiedBy = modifier.Username;

        return Result.Success();
    }
}