using KRAFT.Results.WebApi.Features.UserRoles;

namespace KRAFT.Results.WebApi.Features.Roles;

internal sealed class Role
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public DateTime CreatedOn { get; set; }

    public ICollection<UserRole> UserRoles { get; } = [];
}