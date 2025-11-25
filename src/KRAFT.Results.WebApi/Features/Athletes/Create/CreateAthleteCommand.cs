namespace KRAFT.Results.WebApi.Features.Athletes.Create;

internal sealed record class CreateAthleteCommand(
    string FirstName,
    string LastName,
    int CountryId,
    int? TeamId,
    DateOnly DateOfBirth,
    string Gender);