namespace KRAFT.Results.Contracts.Users;

public sealed record class UserEditDetails(
    string FirstName,
    string LastName,
    string? Email,
    IReadOnlyList<string> Roles);