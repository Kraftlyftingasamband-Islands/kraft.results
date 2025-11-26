namespace KRAFT.Results.WebApi.Features.Teams.Create;

internal sealed record class CreateTeamCommand(string Title, string TitleShort, string TitleFull, int CountryId);