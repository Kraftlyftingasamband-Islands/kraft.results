namespace KRAFT.Results.Contracts.Athletes;

public sealed record class CreateAthleteCommand(
    string FirstName,
    string LastName,
    int CountryId,
    int? TeamId,
    DateOnly DateOfBirth,
    string Gender);