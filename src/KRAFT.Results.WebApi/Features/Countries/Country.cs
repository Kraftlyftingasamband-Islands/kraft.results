using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Teams;

namespace KRAFT.Results.WebApi.Features.Countries;

internal sealed class Country
{
    public int CountryId { get; private set; }

    public string Iso2 { get; private set; } = null!;

    public string Iso3 { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public ICollection<Athlete> Athletes { get; private set; } = [];

    public ICollection<Team> Teams { get; private set; } = [];
}