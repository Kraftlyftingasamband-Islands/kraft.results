namespace KRAFT.Results.Contracts.Clubs;

public sealed record class UpdateClubCommand(string Title, string TitleShort, string TitleFull, string CountryCode);
