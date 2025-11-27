using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Users;

internal static class UserErrors
{
    internal const string InvalidUsernameOrPasswordCode = "Authentication.InvalidUsernameOrPassword";

    internal static readonly Error InvalidUsernameOrPassword = new(
        Code: InvalidUsernameOrPasswordCode,
        Description: "Invalid username or password.");
}