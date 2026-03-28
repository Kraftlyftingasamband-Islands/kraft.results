namespace KRAFT.Results.Contracts.Users;

public sealed record class UserSummary(int UserId, string Name, string? Email, DateTime CreatedOn, IReadOnlyList<string> Roles);