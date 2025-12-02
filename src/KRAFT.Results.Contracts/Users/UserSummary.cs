namespace KRAFT.Results.Contracts.Users;

public sealed record class UserSummary(string Name, string? Email, DateTime CreatedOn, IReadOnlyList<string> Roles);