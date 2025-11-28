namespace KRAFT.Results.Contracts.Users;

public sealed record class LoginCommand(string Username, string Password);