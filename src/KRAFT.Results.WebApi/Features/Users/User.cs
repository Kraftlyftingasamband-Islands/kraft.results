using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.UserRoles;
using KRAFT.Results.WebApi.Features.Users.Infrastructure;
using KRAFT.Results.WebApi.ValueObjects;

namespace KRAFT.Results.WebApi.Features.Users;

internal sealed class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string? Email { get; set; }

    public string Password { get; set; } = null!;

    public string? Firstname { get; set; }

    public string? Lastname { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public string CreatedBy { get; set; } = null!;

    public int? FacebookUserId { get; set; }

    public ICollection<UserRole> UserRoles { get; } = [];

    internal static Result<User> Create(string userName, string firstName, string lastName, Email email, string password)
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
            Password = PasswordHasher.Hash(password),
            CreatedOn = DateTime.UtcNow,
        };

        return user;
    }
}