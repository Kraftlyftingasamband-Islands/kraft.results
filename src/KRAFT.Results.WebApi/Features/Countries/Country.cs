using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Teams;

namespace KRAFT.Results.WebApi.Features.Countries;

internal sealed class Country
{
    public int CountryId { get; set; }

    public string Iso2 { get; set; } = null!;

    public string Iso3 { get; set; } = null!;

    public string Name { get; set; } = null!;

    public ICollection<Athlete> Athletes { get; set; } = [];

    public ICollection<Team> Teams { get; set; } = [];
}