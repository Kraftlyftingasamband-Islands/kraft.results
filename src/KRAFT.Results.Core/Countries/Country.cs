using KRAFT.Results.Core.Athletes;
using KRAFT.Results.Core.Teams;

namespace KRAFT.Results.Core.Countries;

internal class Country
{
    public int CountryId { get; set; }

    public string Iso2 { get; set; } = null!;

    public string Iso3 { get; set; } = null!;

    public string Name { get; set; } = null!;

    public virtual ICollection<Athlete> Athletes { get; set; } = [];

    public virtual ICollection<Team> Teams { get; set; } = [];
}