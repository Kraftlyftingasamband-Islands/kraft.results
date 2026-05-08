namespace KRAFT.Results.Contracts.Teams;

public sealed record class CreateTeamCommand(string Title, string TitleShort, string TitleFull, string CountryCode);