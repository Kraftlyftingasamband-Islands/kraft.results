namespace KRAFT.Results.Core.Athletes.Create;

public sealed record class CreateAthleteCommand(
    string Firstname,
    string Lastname,
    string Gender,
    int CountryId,
    DateOnly? DateOfBirth,
    int? TeamId,
    string? ProfileImageFilename);