namespace KRAFT.Results.Core.Athletes.AddAthlete;

public sealed record class CreateAthleteCommand(
    string FirstName,
    string LastName,
    string Gender,
    int CountryId,
    DateOnly? DateOfBirth,
    int? TeamId,
    string? ProfileImageFilename);