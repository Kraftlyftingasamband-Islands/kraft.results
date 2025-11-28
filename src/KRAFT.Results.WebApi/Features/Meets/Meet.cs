using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.MeetTypes;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Photos;
using KRAFT.Results.WebApi.Features.Users;

namespace KRAFT.Results.WebApi.Features.Meets;

internal sealed class Meet
{
    internal const int StartDateMinimumYear = 1900;

    // For EF core
    private Meet()
    {
    }

    public int MeetId { get; set; }

    public required string Title { get; set; }

    public required string Slug { get; set; }

    public required DateTime StartDate { get; set; }

    public required DateTime EndDate { get; set; }

    public int MeetTypeId { get; set; }

    public bool CalcPlaces { get; set; }

    public string? Text { get; set; }

    public string? Location { get; set; }

    public bool PublishedResults { get; set; }

    public int ResultModeId { get; set; }

    public bool PublishedInCalendar { get; set; }

    public bool IsInTeamCompetition { get; set; }

    public bool ShowWilks { get; set; }

    public bool ShowTeamPoints { get; set; }

    public bool ShowBodyWeight { get; set; }

    public bool ShowTeams { get; set; }

    public bool RecordsPossible { get; set; }

    public bool IsRaw { get; set; }

    public required DateTime CreatedOn { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public required string CreatedBy { get; set; }

    public MeetType MeetType { get; set; } = null!;

    public ICollection<Participation> Participations { get; } = [];

    public ICollection<Photo> Photos { get; } = [];

    internal static Result<Meet> Create(User creator, MeetType type, string title, DateOnly startDate)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return MeetErrors.EmptyTitle;
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
            Slug = ValueObjects.Slug.Create(title),
            CreatedOn = DateTime.UtcNow,
            CreatedBy = creator.Username,
        };

        return meet;
    }
}