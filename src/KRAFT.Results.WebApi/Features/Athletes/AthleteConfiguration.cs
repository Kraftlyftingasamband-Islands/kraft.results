using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal sealed class AthleteConfiguration : IEntityTypeConfiguration<Athlete>
{
    public void Configure(EntityTypeBuilder<Athlete> builder)
    {
        builder.ToTable("Athletes", "dbo");

        builder.HasIndex(e => new { e.Firstname, e.Lastname, e.DateOfBirth }, "IX_Athletes_NameYearUnique").IsUnique();

        builder.HasIndex(e => e.Slug, "IX_Athletes_Slug_Unique").IsUnique();

        builder.HasIndex(e => new { e.CountryId, e.AthleteId }, "_dta_index_Athletes_20_971150505__K8_K1");

        builder.Property(e => e.CountryId)
            .HasDefaultValue(352, "DF_Athletes_CountryId");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(50)
            .HasDefaultValue("klaus", "DF_Athletes_CreatedBy");

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_Athletes_CreatedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.Firstname)
            .HasMaxLength(50);

        builder.Property(e => e.Gender)
            .HasMaxLength(1)
            .IsUnicode(false)
            .HasDefaultValue("m", "DF_Athletes_Gender");

        builder.Property(e => e.Lastname)
            .HasMaxLength(50);

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(50)
            .HasDefaultValue("klaus", "DF_Athletes_ModifiedBy");

        builder.Property(e => e.ModifiedOn)
            .HasDefaultValueSql("(getdate())", "DF_Athletes_ModifiedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.ProfileImageFilename)
            .HasMaxLength(200);

        builder.Property(e => e.Slug).HasMaxLength(50);

        builder.HasOne(d => d.Country).WithMany(p => p.Athletes)
            .HasForeignKey(d => d.CountryId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Athletes_Countries");

        builder.HasOne(d => d.Team).WithMany(p => p.Athletes)
            .HasForeignKey(d => d.TeamId)
            .HasConstraintName("FK_Athletes_Teams");
    }
}