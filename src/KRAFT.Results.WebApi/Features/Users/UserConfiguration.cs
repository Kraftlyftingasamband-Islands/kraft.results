using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.WebApi.Features.Users;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", "dbo");

        builder.HasIndex(e => e.UserId, "IX_Users_UsernameUnique").IsUnique();

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(50)
            .HasDefaultValue("klaus", "DF_Users_CreatedBy");

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_Users_Created")
            .HasColumnType("datetime");

        builder.Property(e => e.Email)
            .HasMaxLength(100);

        builder.Property(e => e.Firstname)
            .HasMaxLength(50);

        builder.Property(e => e.Lastname)
            .HasMaxLength(50);

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(50)
            .HasDefaultValue("klaus", "DF_Users_ModifiedBy");

        builder.Property(e => e.ModifiedOn)
            .HasDefaultValueSql("(getdate())", "DF_Users_Modified")
            .HasColumnType("datetime");

        builder.Property(e => e.Password)
            .HasMaxLength(128);

        builder.Property(e => e.Username)
            .HasMaxLength(50);
    }
}