using KRAFT.Results.Core.UserRoles;

namespace KRAFT.Results.Core.Roles;

internal sealed class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public ICollection<UserRole> UserRoles { get; } = [];
}