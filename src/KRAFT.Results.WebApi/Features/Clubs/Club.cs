using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.ValueObjects;

namespace KRAFT.Results.WebApi.Features.Clubs;

internal sealed class Club : AggregateRoot
{
    internal const int TitleMaxLength = 50;
    internal const int TitleFullMaxLength = 100;
    private const int ShortTitleLength = 3;

    // For EF core
    private Club()
    {
    }

    public int ClubId { get; private set; }

    public string Title { get; private set; } = null!;

    public string TitleShort { get; private set; } = null!;

    public string TitleFull { get; private set; } = null!;

    public string? LogoImageFilename { get; private set; }

    public string Slug { get; private set; } = null!;

    public DateTime CreatedOn { get; private set; }

    public DateTime ModifiedOn { get; private set; }

    public string ModifiedBy { get; private set; } = null!;

    public string CreatedBy { get; private set; } = null!;

    public ICollection<Athlete> Athletes { get; } = [];

    public Country Country { get; private set; } = Country.Iceland;

    public ICollection<Participation> Participations { get; } = [];

    internal static Result<Club> Create(User creator, string title, string titleShort, string titleFull, Country country)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return ClubErrors.EmptyTitle;
        }

        if (title.Length > TitleMaxLength)
        {
            return ClubErrors.TitleTooLong;
        }

        if (string.IsNullOrWhiteSpace(titleShort) || titleShort.Length != ShortTitleLength)
        {
            return ClubErrors.InvalidTitleShort;
        }

        if (string.IsNullOrWhiteSpace(titleFull))
        {
            return ClubErrors.EmptyTitleFull;
        }

        if (titleFull.Length > TitleFullMaxLength)
        {
            return ClubErrors.TitleFullTooLong;
        }

        Club club = new()
        {
            Title = title,
            TitleShort = titleShort,
            TitleFull = titleFull,
            Country = country,
            Slug = ValueObjects.Slug.Create(title),
            CreatedOn = DateTime.UtcNow,
            CreatedBy = creator.Username,
        };

        return club;
    }

    internal Result Update(User modifier, string title, string titleShort, string titleFull, Country country)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return ClubErrors.EmptyTitle;
        }

        if (title.Length > TitleMaxLength)
        {
            return ClubErrors.TitleTooLong;
        }

        if (string.IsNullOrWhiteSpace(titleShort) || titleShort.Length != ShortTitleLength)
        {
            return ClubErrors.InvalidTitleShort;
        }

        if (string.IsNullOrWhiteSpace(titleFull))
        {
            return ClubErrors.EmptyTitleFull;
        }

        if (titleFull.Length > TitleFullMaxLength)
        {
            return ClubErrors.TitleFullTooLong;
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
