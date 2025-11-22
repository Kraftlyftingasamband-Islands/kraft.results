using KRAFT.Results.Core.UserRoles;

namespace KRAFT.Results.Core.Users;

internal class User
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

    public virtual ICollection<UserRole> UserRoles { get; } = [];
}