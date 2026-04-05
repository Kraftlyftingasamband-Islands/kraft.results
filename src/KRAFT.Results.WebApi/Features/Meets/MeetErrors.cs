using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Meets;

internal static class MeetErrors
{
    internal const string MeetNotFoundCode = "Meets.NotFound";

    internal const string MeetExistsCode = "Meets.AlreadyExists";

    internal const string MeetHasParticipationsCode = "Meets.HasParticipations";

    internal const string ParticipationNotFoundCode = "Meets.ParticipationNotFound";

    internal const string InvalidBodyWeightCode = "Meets.InvalidBodyWeight";

    internal const string AthleteAlreadyRegisteredCode = "Meets.AthleteAlreadyRegistered";

    internal const string AgeCategoryNotFoundCode = "Meets.AgeCategoryNotFound";

    internal const string NoMatchingWeightCategoryCode = "Meets.NoMatchingWeightCategory";

    internal static readonly Error EmptyTitle = new(
        "Meets.TitleIsEmpty",
        "Title cannot be empty.");

    internal static readonly Error MeetTypeNotFound = new(
        "Meets.MeetTypeNotFound",
        "Meet type not found in database");

    internal static readonly Error MeetNotFound = new(
        MeetNotFoundCode,
        "Meet not found.");

    internal static readonly Error TitleTooLong = new(
        "Meets.TitleTooLong",
        $"Title cannot exceed {Meet.TitleMaxLength} characters.");

    internal static readonly Error MeetHasParticipations = new(
        MeetHasParticipationsCode,
        "Cannot delete a meet that has participations.");

    internal static readonly Error AgeCategoryNotFound = new(
        AgeCategoryNotFoundCode,
        "Age category not found.");

    internal static readonly Error WeightCategoryNotFound = new(
        "Meets.WeightCategoryNotFound",
        "Weight category not found.");

    internal static readonly Error ParticipationNotFound = new(
        ParticipationNotFoundCode,
        "Participation not found.");

    internal static readonly Error InvalidBodyWeight = new(
        "Meets.InvalidBodyWeight",
        "Body weight must be greater than zero.");

    internal static readonly Error AthleteAlreadyRegistered = new(
        AthleteAlreadyRegisteredCode,
        "Athlete is already registered in this meet.");

    internal static readonly Error NoMatchingWeightCategory = new(
        NoMatchingWeightCategoryCode,
        "No matching weight category found for the given body weight.");

    internal static readonly Error AttemptOutOfOrder = new(
        "Meets.AttemptOutOfOrder",
        "Attempt weight must not decrease from a previous round or increase beyond a later round.");

    internal static Error InvalidStartDate(DateOnly startDate) => new(
        "Meets.InvalidStartDate",
        $"Start date '{startDate:yyyy-MM-dd}' is invalid. The year must be 1900 or later.");

    internal static Error MeetExists(string title, DateOnly startDate) => new(
        MeetExistsCode,
        $"A meet with the title '{title}' and start date '{startDate:yyyy-MM-dd}' already exists.");
}