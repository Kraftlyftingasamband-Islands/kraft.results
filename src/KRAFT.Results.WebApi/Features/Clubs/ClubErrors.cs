using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Clubs;

internal static class ClubErrors
{
    internal const string ClubNotFoundCode = "Teams.NotFound";
    internal const string EmptyTitleCode = "Teams.TitleIsEmpty";
    internal const string InvalidTitleShortCode = "Teams.InvalidTitleShort";
    internal const string EmptyTitleFullCode = "Teams.EmptyTitleFull";
    internal const string TitleTooLongCode = "Teams.TitleTooLong";
    internal const string ShortTitleExistsCode = "Teams.ShortTitleExists";
    internal const string TitleExistsCode = "Teams.TitleExists";
    internal const string ClubHasAthletesCode = "Teams.HasAthletes";
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
        $"Title cannot exceed {Club.TitleMaxLength} characters.");

    internal static readonly Error TitleFullTooLong = new(
        TitleFullTooLongCode,
        $"Full title cannot exceed {Club.TitleFullMaxLength} characters.");

    internal static readonly Error ClubNotFound = new(
        ClubNotFoundCode,
        "Club not found.");

    internal static readonly Error ClubHasAthletes = new(
        ClubHasAthletesCode,
        "Cannot delete a club that has athletes assigned.");

    internal static readonly Error ShortTitleExists = new(
        ShortTitleExistsCode,
        "A club with that short title already exists.");

    internal static readonly Error TitleExists = new(
        TitleExistsCode,
        "A club with that title already exists.");

    internal static Error ClubDoesNotExist(int id) => new(
        "Teams.TeamDoesNotExist",
        $"Club with Id {id} does not exist.");
}
