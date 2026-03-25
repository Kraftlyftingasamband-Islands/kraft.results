namespace KRAFT.Results.Contracts.Athletes;

public sealed record class AthleteEditDetails(
    string FirstName,
    string LastName,
    DateOnly? DateOfBirth,
    string Gender,
    int CountryId,
    int? TeamId);