namespace KRAFT.Results.WebApi.Features.Users.Login;

internal sealed record class AuthenticatedResponse(string AccessToken, string RefreshToken);