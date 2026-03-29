using System.ComponentModel.DataAnnotations;

namespace KRAFT.Results.Contracts.Users;

public sealed record class ChangeUserRoleCommand([MaxLength(50)] string Role);