using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Teams;

internal static class TeamErrors
{
    internal static Error EmptyTitle => new(
        "Teams.TitleIsEmpty",
        "Title cannot be empty.");

    internal static Error InvalidTitleShort => new(
        "Teams.InvalidTitleShort",
        "Short title must be exactly 3 alphabetic characters.");

    internal static Error EmptyTitleFull => new(
        "Teams.EmptyTitleFull",
        "Full title cannot be empty.");

    internal static Error ShortTitleExists(string titleShort) => new(
        "Athletes.ShortTitleExists",
        $"Team with short title {titleShort} already exists.");

    internal static Error TeamDoesNotExist(int id) => new(
        "Athletes.TeamDoesNotExist",
        $"Team with Id {id} does not exist.");
}