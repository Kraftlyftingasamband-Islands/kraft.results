using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Teams;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal sealed class Athlete
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

    public Country Country { get; set; } = null!;

    public ICollection<Participation> Participations { get; } = [];

    public Team? Team { get; set; }
}