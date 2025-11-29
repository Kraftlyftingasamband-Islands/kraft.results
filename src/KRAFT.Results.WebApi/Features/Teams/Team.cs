using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Users;

namespace KRAFT.Results.WebApi.Features.Teams;

internal sealed class Team
{
    private const int ShortTitleLength = 3;

    // For EF core
    private Team()
    {
    }

    public int TeamId { get; set; }

    public required string Title { get; set; }

    public required string TitleShort { get; set; }

    public required string TitleFull { get; set; }

    public int? CountryId { get; set; }

    public string? LogoImageFilename { get; set; }

    public required string Slug { get; set; }

    public required DateTime CreatedOn { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public required string CreatedBy { get; set; } = null!;

    public ICollection<Athlete> Athletes { get; } = [];

    public Country? Country { get; set; }

    public ICollection<Participation> Participations { get; } = [];

    internal static Result<Team> Create(User creator, string title, string titleShort, string titleFull, Country country)
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
            Slug = ValueObjects.Slug.Create(title),
            CreatedOn = DateTime.UtcNow,
            CreatedBy = creator.Username,
        };

        return team;
    }
}