using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal static class AthleteErrors
{
    internal const string AlreadyExistsCode = "Athletes.AlreadyExists";
    internal const string AthleteNotFoundCode = "Athletes.NotFound";
    internal const string AthleteHasParticipationsCode = "Athletes.HasParticipations";

    internal static readonly Error AlreadyExists = new(
        AlreadyExistsCode,
        "An athlete with that name and date of birth already exists.");

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

    internal static Error AthleteHasParticipations => new(
        AthleteHasParticipationsCode,
        "Cannot delete an athlete that has participations.");
}