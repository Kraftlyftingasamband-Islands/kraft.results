using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.UserRoles;
using KRAFT.Results.WebApi.ValueObjects;

namespace KRAFT.Results.WebApi.Features.Users;

internal sealed class User
{
    // For EF core
    private User()
    {
    }

    public int UserId { get; private set; }

    public string Username { get; private set; } = null!;

    public string? Email { get; private set; }

    public Password Password { get; set; } = null!;

    public string? Firstname { get; private set; }

    public string? Lastname { get; private set; }

    public DateTime CreatedOn { get; private set; }

    public DateTime ModifiedOn { get; private set; }

    public string ModifiedBy { get; private set; } = null!;

    public string CreatedBy { get; private set; } = null!;

    public int? FacebookUserId { get; private set; }

    public ICollection<UserRole> UserRoles { get; } = [];

    internal static Result<User> Create(User creator, string userName, string firstName, string lastName, Email email, Password password)
    {
        if (string.IsNullOrWhiteSpace(userName))
        {
            return UserErrors.UserNameEmpty;
        }

        if (string.IsNullOrWhiteSpace(firstName))
        {
            return UserErrors.FirstNameEmpty;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return UserErrors.LastNameEmpty;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return UserErrors.PasswordEmpty;
        }

        User user = new()
        {
            Username = userName,
            Firstname = firstName,
            Lastname = lastName,
            Email = email,
            Password = password,
            CreatedOn = DateTime.UtcNow,
            CreatedBy = creator.Username,
        };

        return user;
    }

    internal Result ChangePassword(string currentPassword, string newPassword, string confirmNewPassword)
    {
        if (newPassword != confirmNewPassword)
        {
            return UserErrors.PasswordsDoNotMatch;
        }

        Result<Password> hashedNewPassword = ValueObjects.Password.Hash(newPassword);

        if (hashedNewPassword.IsFailure)
        {
            return Result.Failure(hashedNewPassword.Error);
        }

        if (!Password.IsHashed)
        {
            Password = ValueObjects.Password.Hash(Password);
        }

        if (!Password.Verify(currentPassword))
        {
            return UserErrors.IncorrectCurrentPassword;
        }

        Password = hashedNewPassword.FromResult();
        ModifiedOn = DateTime.UtcNow;
        ModifiedBy = Username;

        return Result.Success();
    }

    internal Result Update(User modifier, string firstName, string lastName, Email email)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            return UserErrors.FirstNameEmpty;
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            return UserErrors.LastNameEmpty;
        }

        Firstname = firstName;
        Lastname = lastName;
        Email = email;
        ModifiedOn = DateTime.UtcNow;
        ModifiedBy = modifier.Username;

        return Result.Success();
    }
}