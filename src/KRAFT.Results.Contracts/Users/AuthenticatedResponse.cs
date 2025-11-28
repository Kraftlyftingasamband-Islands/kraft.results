namespace KRAFT.Results.Contracts.Users;

public sealed record class AuthenticatedResponse(string AccessToken, string RefreshToken);