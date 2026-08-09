namespace KRAFT.Results.Contracts.Clubs;

public sealed record class CreateClubCommand(string Title, string TitleShort, string TitleFull, string CountryCode);
