namespace KRAFT.Results.Contracts.Athletes;

public sealed record class AthleteFormInitialValues(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string Gender,
    string CountryCode,
    int TeamId);