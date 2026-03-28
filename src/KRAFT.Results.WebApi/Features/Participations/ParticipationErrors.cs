using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Participations;

internal static class ParticipationErrors
{
    internal static Error AthleteIdMustBePositive => new(
        "Participations.AthleteIdMustBePositive",
        "Athlete ID must be a positive number.");

    internal static Error MeetIdMustBePositive => new(
        "Participations.MeetIdMustBePositive",
        "Meet ID must be a positive number.");

    internal static Error WeightCategoryIdMustBePositive => new(
        "Participations.WeightCategoryIdMustBePositive",
        "Weight category ID must be a positive number.");

    internal static Error BodyWeightMustNotBeNegative => new(
        "Participations.BodyWeightMustNotBeNegative",
        "Body weight must not be negative.");
}