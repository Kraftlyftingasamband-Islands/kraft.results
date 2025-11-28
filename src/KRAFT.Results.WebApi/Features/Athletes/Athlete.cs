using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Teams;
using KRAFT.Results.WebApi.Features.Users;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal sealed class Athlete
{
    // For EF core
    private Athlete()
    {
    }

    public int AthleteId { get; set; }

    public required string Firstname { get; set; }

    public required string Lastname { get; set; }

    public required string? Slug { get; set; }

    public required DateOnly? DateOfBirth { get; set; }

    public required string Gender { get; init; }

    public required DateTime CreatedOn { get; init; }

    public DateTime ModifiedOn { get; set; }

    public string? ProfileImageFilename { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public required string CreatedBy { get; set; }

    public int CountryId { get; set; }

    public Country Country { get; set; } = null!;

    public int? TeamId { get; set; }

    public Team? Team { get; set; }

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

        if (!gender.Equals("m", StringComparison.OrdinalIgnoreCase) && !gender.Equals("f", StringComparison.OrdinalIgnoreCase))
        {
            return AthleteErrors.InvalidGender;
        }

        return new Athlete
        {
            Firstname = firstName,
            Lastname = lastName,
            Gender = gender,
            DateOfBirth = dateOfBirth,
            Country = country,
            Team = team,
            Slug = ValueObjects.Slug.Create($"{firstName} {lastName}"),
            CreatedOn = DateTime.UtcNow,
            CreatedBy = creator.Username,
        };
    }
}