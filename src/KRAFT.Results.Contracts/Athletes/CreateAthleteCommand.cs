namespace KRAFT.Results.Contracts.Athletes;

public sealed record class CreateAthleteCommand(
    string FirstName,
    string LastName,
    string CountryCode,
    int? TeamId,
    DateOnly DateOfBirth,
    string Gender);