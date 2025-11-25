using KRAFT.Results.WebApi.Features.MeetTypes;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Photos;

namespace KRAFT.Results.WebApi.Features.Meets;

internal sealed class Meet
{
    public int MeetId { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

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

    public DateTime CreatedOn { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public string CreatedBy { get; set; } = null!;

    public MeetType MeetType { get; set; } = null!;

    public ICollection<Participation> Participations { get; } = [];

    public ICollection<Photo> Photos { get; } = [];
}