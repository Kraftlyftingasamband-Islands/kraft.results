using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Meets;

internal static class MeetErrors
{
    internal const string MeetExistsCode = "Meets.AlreadyExists";

    internal static readonly Error EmptyTitle = new(
        "Meets.TitleIsEmpty",
        "Title cannot be empty.");

    internal static readonly Error MeetTypeNotFound = new(
        "Meets.MeetTypeNotFound",
        "Meet type not found in database");

    internal static Error InvalidStartDate(DateOnly startDate) => new(
        "Meets.InvalidStartDate",
        $"Start date '{startDate:yyyy-MM-dd}' is invalid. The year must be 1900 or later.");

    internal static Error MeetExists(string title, DateOnly startDate) => new(
        MeetExistsCode,
        $"A meet with the title '{title}' and start date '{startDate:yyyy-MM-dd}' already exists.");
}