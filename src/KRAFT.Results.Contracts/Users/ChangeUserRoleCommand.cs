namespace KRAFT.Results.Contracts.Users;

public sealed record class ChangeUserRoleCommand(IReadOnlyList<string> Roles);