using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Meets;

internal static class MeetErrors
{
    internal const string MeetNotFoundCode = "Meets.NotFound";

    internal const string MeetExistsCode = "Meets.AlreadyExists";

    internal const string MeetHasParticipationsCode = "Meets.HasParticipations";

    internal const string ParticipationNotFoundCode = "Meets.ParticipationNotFound";

    internal const string AthleteAlreadyRegisteredCode = "Meets.AthleteAlreadyRegistered";

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

    internal static readonly Error WeightCategoryNotFound = new(
        "Meets.WeightCategoryNotFound",
        "Weight category not found.");

    internal static readonly Error ParticipationNotFound = new(
        ParticipationNotFoundCode,
        "Participation not found.");

    internal static readonly Error AthleteAlreadyRegistered = new(
        AthleteAlreadyRegisteredCode,
        "Athlete is already registered in this meet.");

    internal static Error InvalidStartDate(DateOnly startDate) => new(
        "Meets.InvalidStartDate",
        $"Start date '{startDate:yyyy-MM-dd}' is invalid. The year must be 1900 or later.");

    internal static Error MeetExists(string title, DateOnly startDate) => new(
        MeetExistsCode,
        $"A meet with the title '{title}' and start date '{startDate:yyyy-MM-dd}' already exists.");
}