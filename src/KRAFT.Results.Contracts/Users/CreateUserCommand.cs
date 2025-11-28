namespace KRAFT.Results.Contracts.Users;

public sealed record class CreateUserCommand(
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    string Password);