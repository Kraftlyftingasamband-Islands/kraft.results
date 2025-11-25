using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.Core.Features.Roles;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "dbo");

        builder.Property(e => e.RoleId)
            .ValueGeneratedNever();

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_Roles_CreatedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.RoleName).HasMaxLength(50);
    }
}