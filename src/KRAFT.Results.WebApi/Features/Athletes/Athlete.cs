using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Teams;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.ValueObjects;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal sealed class Athlete : AggregateRoot
{
    // For EF core
    private Athlete()
    {
    }

    public int AthleteId { get; private set; }

    public string Firstname { get; private set; } = default!;

    public string Lastname { get; private set; } = default!;

    public string Slug { get; private set; } = default!;

    public DateOnly? DateOfBirth { get; private set; } = default!;

    public Gender Gender { get; private set; } = default!;

    public DateTime CreatedOn { get; private set; }

    public DateTime ModifiedOn { get; private set; }

    public string? ProfileImageFilename { get; private set; }

    public string ModifiedBy { get; private set; } = default!;

    public string CreatedBy { get; private set; } = default!;

    public int CountryId { get; private set; }

    public Country Country { get; private set; } = default!;

    public int? TeamId { get; private set; }

    public Team? Team { get; private set; }

    public ICollection<Participation> Participations { get; } = [];

    internal static Result<Athlete> Create(User creator, string firstName, string lastName, string gender, Country country, DateOnly? dateOfBirth, Team? team)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return AthleteErrors.FirstNameIsEmpty;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return AthleteErrors.LastNameIsEmpty;
        }

        if (!Gender.TryParse(gender, out Gender? parsedGender))
        {
            return AthleteErrors.InvalidGender;
        }

        if (dateOfBirth.HasValue && dateOfBirth.Value > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return AthleteErrors.DateOfBirthInFuture;
        }

        return new Athlete
        {
            Firstname = firstName,
            Lastname = lastName,
            Gender = parsedGender,
            DateOfBirth = dateOfBirth,
            Country = country,
            Team = team,
            Slug = ValueObjects.Slug.Create($"{firstName} {lastName}"),
            CreatedOn = DateTime.UtcNow,
            CreatedBy = creator.Username,
        };
    }

    internal Result Update(User modifier, string firstName, string lastName, string gender, Country country, DateOnly? dateOfBirth, Team? team)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return AthleteErrors.FirstNameIsEmpty;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return AthleteErrors.LastNameIsEmpty;
        }

        if (!Gender.TryParse(gender, out Gender? parsedGender))
        {
            return AthleteErrors.InvalidGender;
        }

        if (dateOfBirth.HasValue && dateOfBirth.Value > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return AthleteErrors.DateOfBirthInFuture;
        }

        Firstname = firstName;
        Lastname = lastName;
        Slug = ValueObjects.Slug.Create($"{firstName} {lastName}");
        Gender = parsedGender;
        DateOfBirth = dateOfBirth;
        Country = country;
        Team = team;
        ModifiedOn = DateTime.UtcNow;
        ModifiedBy = modifier.Username;

        return Result.Success();
    }
}