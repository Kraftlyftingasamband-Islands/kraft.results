namespace KRAFT.Results.WebApi.Features.Users.Login;

internal sealed record class LoginCommand(string Username, string Password);