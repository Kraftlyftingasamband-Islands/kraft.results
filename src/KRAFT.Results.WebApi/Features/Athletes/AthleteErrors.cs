using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal static class AthleteErrors
{
    internal const string AlreadyExistsCode = "Athletes.AlreadyExists";
    internal const string AthleteNotFoundCode = "Athletes.NotFound";

    internal static Error FirstNameIsEmpty => new(
        "Athletes.FirstNameIsEmpty",
        "First name cannot be empty.");

    internal static Error LastNameIsEmpty => new(
        "Athletes.LastNameIsEmpty",
        "Last name cannot be empty.");

    internal static Error InvalidGender => new(
        "Athletes.InvalidGender",
        "Gender must be 'm' or 'f'.");

    internal static Error DateOfBirthInFuture => new(
        "Athletes.DateOfBirthInFuture",
        "Fæðingardagur má ekki vera í framtíðinni.");

    internal static Error AthleteNotFound => new(
        AthleteNotFoundCode,
        "Athlete not found.");

    internal static Error AlreadyExists(string firstName, string lastName, DateOnly dateOfBirth) => new(
        AlreadyExistsCode,
        $"An athlete with the first name '{firstName}', last name '{lastName}' and date of birth {dateOfBirth:yyyy-MM-dd} already exists");
}