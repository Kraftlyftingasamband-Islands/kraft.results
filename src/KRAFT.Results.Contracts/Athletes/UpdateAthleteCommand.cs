namespace KRAFT.Results.Contracts.Athletes;

public sealed record class UpdateAthleteCommand(
    string FirstName,
    string LastName,
    string CountryCode,
    int? TeamId,
    DateOnly DateOfBirth,
    string Gender);