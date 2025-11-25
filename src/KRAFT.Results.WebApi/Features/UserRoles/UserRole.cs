using KRAFT.Results.Core.Features.Roles;
using KRAFT.Results.Core.Features.Users;

namespace KRAFT.Results.Core.Features.UserRoles;

internal sealed class UserRole
{
    public int UserRoleId { get; set; }

    public int UserId { get; set; }

    public int RoleId { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public string CreatedBy { get; set; } = null!;

    public Role Role { get; set; } = null!;

    public User User { get; set; } = null!;
}