using KRAFT.Results.Core.Athletes;
using KRAFT.Results.Core.Countries;
using KRAFT.Results.Core.Participations;

namespace KRAFT.Results.Core.Teams;

internal class Team
{
    public int TeamId { get; set; }

    public string Title { get; set; } = null!;

    public string TitleShort { get; set; } = null!;

    public int? CountryId { get; set; }

    public string? LogoImageFilename { get; set; }

    public string? Slug { get; set; }

    public string TitleFull { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public string CreatedBy { get; set; } = null!;

    public virtual ICollection<Athlete> Athletes { get; } = [];

    public virtual Country? Country { get; set; }

    public virtual ICollection<Participation> Participations { get; } = [];
}