namespace KRAFT.Results.Contracts.Athletes;

public sealed record class AthleteFormValues(
    string FirstName,
    string LastName,
    DateOnly DateOfBirth,
    string Gender,
    string CountryCode,
    int? TeamId);