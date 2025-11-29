using KRAFT.Results.WebApi.Features.Roles;
using KRAFT.Results.WebApi.Features.Users;

namespace KRAFT.Results.WebApi.Features.UserRoles;

internal sealed class UserRole
{
    public int UserRoleId { get; private set; }

    public int UserId { get; private set; }

    public int RoleId { get; private set; }

    public DateTime CreatedOn { get; private set; }

    public DateTime ModifiedOn { get; private set; }

    public string ModifiedBy { get; private set; } = null!;

    public string CreatedBy { get; private set; } = null!;

    public Role Role { get; private set; } = null!;

    public User User { get; private set; } = null!;
}