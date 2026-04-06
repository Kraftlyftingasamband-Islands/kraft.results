using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Users;

namespace KRAFT.Results.WebApi.Features.Teams;

internal sealed class Team : AggregateRoot
{
    internal const int TitleMaxLength = 50;
    internal const int TitleFullMaxLength = 100;
    private const int ShortTitleLength = 3;

    // For EF core
    private Team()
    {
    }

    public int TeamId { get; private set; }

    public string Title { get; private set; } = null!;

    public string TitleShort { get; private set; } = null!;

    public string TitleFull { get; private set; } = null!;

    public int? CountryId { get; private set; }

    public string? LogoImageFilename { get; private set; }

    public string Slug { get; private set; } = null!;

    public DateTime CreatedOn { get; private set; }

    public DateTime ModifiedOn { get; private set; }

    public string ModifiedBy { get; private set; } = null!;

    public string CreatedBy { get; private set; } = null!;

    public ICollection<Athlete> Athletes { get; } = [];

    public Country? Country { get; private set; }

    public ICollection<Participation> Participations { get; } = [];

    internal static Result<Team> Create(User creator, string title, string titleShort, string titleFull, Country country)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return TeamErrors.EmptyTitle;
        }

        if (title.Length > TitleMaxLength)
        {
            return TeamErrors.TitleTooLong;
        }

        if (string.IsNullOrWhiteSpace(titleShort) || titleShort.Length != ShortTitleLength)
        {
            return TeamErrors.InvalidTitleShort;
        }

        if (string.IsNullOrWhiteSpace(titleFull))
        {
            return TeamErrors.EmptyTitleFull;
        }

        if (titleFull.Length > TitleFullMaxLength)
        {
            return TeamErrors.TitleFullTooLong;
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

    internal Result Update(User modifier, string title, string titleShort, string titleFull, Country country)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return TeamErrors.EmptyTitle;
        }

        if (title.Length > TitleMaxLength)
        {
            return TeamErrors.TitleTooLong;
        }

        if (string.IsNullOrWhiteSpace(titleShort) || titleShort.Length != ShortTitleLength)
        {
            return TeamErrors.InvalidTitleShort;
        }

        if (string.IsNullOrWhiteSpace(titleFull))
        {
            return TeamErrors.EmptyTitleFull;
        }

        if (titleFull.Length > TitleFullMaxLength)
        {
            return TeamErrors.TitleFullTooLong;
        }

        Title = title;
        TitleShort = titleShort;
        TitleFull = titleFull;
        Country = country;
        ModifiedOn = DateTime.UtcNow;
        ModifiedBy = modifier.Username;

        return Result.Success();
    }
}