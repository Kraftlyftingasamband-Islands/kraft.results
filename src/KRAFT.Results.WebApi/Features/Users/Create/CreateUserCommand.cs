namespace KRAFT.Results.WebApi.Features.Users.Create;

internal sealed record class CreateUserCommand(
    string UserName,
    string FirstName,
    string LastName,
    string Email,
    string Password);