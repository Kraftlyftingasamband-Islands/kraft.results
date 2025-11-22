using KRAFT.Results.Core.Countries;
using KRAFT.Results.Core.Participations;
using KRAFT.Results.Core.Teams;

namespace KRAFT.Results.Core.Athletes;

internal class Athlete
{
    public int AthleteId { get; set; }

    public string Firstname { get; set; } = null!;

    public string Lastname { get; set; } = null!;

    public string? Slug { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string Gender { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public DateTime ModifiedOn { get; set; }

    public int? TeamId { get; set; }

    public int CountryId { get; set; }

    public string? ProfileImageFilename { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public string CreatedBy { get; set; } = null!;

    public virtual Country Country { get; set; } = null!;

    public virtual ICollection<Participation> Participations { get; } = [];

    public virtual Team? Team { get; set; }
}