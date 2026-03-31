using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.Features.Users;

internal static class UserErrors
{
    internal const string InvalidUsernameOrPasswordCode = "Users.InvalidUsernameOrPassword";
    internal const string UserNameExistsCode = "Users.UserNameExists";
    internal const string EmailExistsCode = "Users.EmailExists";
    internal const string UserNameClaimMissingCode = "Users.UserNameClaimMissing";
    internal const string UserNotFoundCode = "Users.NotFound";
    internal const string CannotDeleteSelfCode = "Users.CannotDeleteSelf";
    internal const string CannotChangeOwnRoleCode = "Users.CannotChangeOwnRole";
    internal const string RoleNotFoundCode = "Users.RoleNotFound";
    internal const string RolesRequiredCode = "Users.RolesRequired";

    internal static readonly Error InvalidUsernameOrPassword = new(
        Code: InvalidUsernameOrPasswordCode,
        Description: "Invalid username or password.");

    internal static readonly Error UserNameExists = new(
        Code: UserNameExistsCode,
        Description: "User name already exists");

    internal static readonly Error EmailExists = new(
        Code: EmailExistsCode,
        Description: "There is already a user with that e-mail");

    internal static readonly Error UserNameEmpty = new(
        Code: "Users.UserNameEmpty",
        Description: "Username must contain a value");

    internal static readonly Error FirstNameEmpty = new(
        Code: "Users.FirstNameEmpty",
        Description: "First name must contain a value");

    internal static readonly Error LastNameEmpty = new(
        Code: "Users.LastNameEmpty",
        Description: "Last name must contain a value");

    internal static readonly Error PasswordEmpty = new(
        Code: "Users.PasswordNameEmpty",
        Description: "Password must contain a value");

    internal static readonly Error UserNameClaimMissing = new(
        Code: UserNameClaimMissingCode,
        Description: "The authentication token is missing the required name claim.");

    internal static readonly Error UserNotFound = new(
        Code: UserNotFoundCode,
        Description: "User not found.");

    internal static readonly Error CannotDeleteSelf = new(
        Code: CannotDeleteSelfCode,
        Description: "An admin cannot delete their own account.");

    internal static readonly Error CannotChangeOwnRole = new(
        Code: CannotChangeOwnRoleCode,
        Description: "An admin cannot change their own role.");

    internal static readonly Error RoleNotFound = new(
        Code: RoleNotFoundCode,
        Description: "The specified role does not exist.");

    internal static readonly Error RolesRequired = new(
        Code: RolesRequiredCode,
        Description: "At least one role must be specified.");
}