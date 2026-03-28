namespace KRAFT.Results.Contracts.Users;

public sealed record class UpdateUserCommand(
    string FirstName,
    string LastName,
    string Email);