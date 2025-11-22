using KRAFT.Results.Core.UserRoles;

namespace KRAFT.Results.Core.Roles;

internal class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; } = [];
}