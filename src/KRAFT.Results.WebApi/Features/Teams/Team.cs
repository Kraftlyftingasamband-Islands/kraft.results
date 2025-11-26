using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Participations;

namespace KRAFT.Results.WebApi.Features.Teams;

internal sealed class Team
{
    private const int ShortTitleLength = 3;

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

    public ICollection<Athlete> Athletes { get; } = [];

    public Country? Country { get; set; }

    public ICollection<Participation> Participations { get; } = [];

    internal static Result<Team> Create(string title, string titleShort, string titleFull, Country country)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return TeamErrors.EmptyTitle;
        }

        if (string.IsNullOrWhiteSpace(titleShort) || titleShort.Length != ShortTitleLength)
        {
            return TeamErrors.InvalidTitleShort;
        }

        if (string.IsNullOrWhiteSpace(titleFull))
        {
            return TeamErrors.EmptyTitleFull;
        }

        Team team = new()
        {
            Title = title,
            TitleShort = titleShort,
            TitleFull = titleFull,
            Country = country,
            CreatedOn = DateTime.UtcNow,
        };

        return team;
    }
}