using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Teams;

internal static class TeamErrors
{
    internal const string TeamNotFoundCode = "Teams.NotFound";
    internal const string EmptyTitleCode = "Teams.TitleIsEmpty";
    internal const string InvalidTitleShortCode = "Teams.InvalidTitleShort";
    internal const string EmptyTitleFullCode = "Teams.EmptyTitleFull";
    internal const string TitleTooLongCode = "Teams.TitleTooLong";
    internal const string TeamHasAthletesCode = "Teams.HasAthletes";
    internal const string TitleFullTooLongCode = "Teams.TitleFullTooLong";

    internal static readonly Error EmptyTitle = new(
        EmptyTitleCode,
        "Title cannot be empty.");

    internal static readonly Error InvalidTitleShort = new(
        InvalidTitleShortCode,
        "Short title must be exactly 3 alphabetic characters.");

    internal static readonly Error EmptyTitleFull = new(
        EmptyTitleFullCode,
        "Full title cannot be empty.");

    internal static readonly Error TitleTooLong = new(
        TitleTooLongCode,
        $"Title cannot exceed {Team.TitleMaxLength} characters.");

    internal static readonly Error TitleFullTooLong = new(
        TitleFullTooLongCode,
        $"Full title cannot exceed {Team.TitleFullMaxLength} characters.");

    internal static readonly Error TeamNotFound = new(
        TeamNotFoundCode,
        "Team not found.");

    internal static readonly Error TeamHasAthletes = new(
        TeamHasAthletesCode,
        "Cannot delete a team that has athletes assigned.");

    internal static Error ShortTitleExists(string titleShort) => new(
        "Teams.ShortTitleExists",
        $"Team with short title {titleShort} already exists.");

    internal static Error TeamDoesNotExist(int id) => new(
        "Teams.TeamDoesNotExist",
        $"Team with Id {id} does not exist.");
}