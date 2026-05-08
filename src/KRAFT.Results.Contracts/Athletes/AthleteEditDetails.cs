namespace KRAFT.Results.Contracts.Athletes;

public sealed record class AthleteEditDetails(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string Gender,
    string CountryCode,
    int? TeamId);