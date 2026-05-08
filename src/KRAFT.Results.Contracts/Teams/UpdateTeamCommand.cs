namespace KRAFT.Results.Contracts.Teams;

public sealed record class UpdateTeamCommand(string Title, string TitleShort, string TitleFull, string CountryCode);