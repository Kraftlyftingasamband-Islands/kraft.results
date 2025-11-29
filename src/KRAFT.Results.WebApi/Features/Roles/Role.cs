using KRAFT.Results.WebApi.Features.UserRoles;

namespace KRAFT.Results.WebApi.Features.Roles;

internal sealed class Role
{
    public int RoleId { get; private set; }

    public string RoleName { get; private set; } = null!;

    public DateTime CreatedOn { get; private set; }

    public ICollection<UserRole> UserRoles { get; } = [];
}