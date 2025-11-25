using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal static class AthleteErrors
{
    internal static Error FirstNameIsEmpty => new(
        "Athletes.FirstNameIsEmpty",
        "First name cannot be empty.");

    internal static Error LastNameIsEmpty => new(
        "Athletes.LastNameIsEmpty",
        "Last name cannot be empty.");

    internal static Error InvalidGender => new(
        "Athletes.InvalidGender",
        "Gender must be 'm' or 'f'.");

    internal static Error CountryDoesNotExist(int id) => new(
        "Athletes.CountryDoesNotExist",
        $"Country with Id {id} does not exist.");

    internal static Error TeamDoesNotExist(int id) => new(
        "Athletes.TeamDoesNotExist",
        $"Team with Id {id} does not exist.");
}