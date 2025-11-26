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
}