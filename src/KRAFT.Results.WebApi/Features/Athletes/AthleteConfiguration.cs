using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal sealed class AthleteConfiguration : IEntityTypeConfiguration<Athlete>
{
    public void Configure(EntityTypeBuilder<Athlete> builder)
    {
        builder.ToTable("Athletes", "dbo");

        builder.HasIndex(e => new { e.Firstname, e.Lastname, e.DateOfBirth }, "IX_Athletes_NameYearUnique")
            .IsUnique();

        builder.HasIndex(e => e.Slug, "IX_Athletes_Slug_Unique")
            .IsUnique();

        builder.Property(e => e.Country)
            .HasConversion(x => x.Value, x => Country.Parse(x))
            .HasColumnName("CountryCode")
            .HasMaxLength(3)
            .IsFixedLength()
            .IsUnicode(true)
            .IsRequired()
            .HasDefaultValue(Country.Parse("ISL"), "DF_Athletes_CountryCode");

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
            .HasDefaultValue(Gender.Male, "DF_Athletes_Gender")
            .HasConversion(
                x => x.Value,
                x => Gender.Parse(x));

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

        builder.Property(e => e.Slug)
            .HasMaxLength(50);

        builder.HasOne(d => d.Team)
            .WithMany(p => p.Athletes)
            .HasForeignKey(d => d.TeamId)
            .HasConstraintName("FK_Athletes_Teams");

        builder.HasMany(a => a.Bans)
            .WithOne(b => b.Athlete)
            .HasForeignKey(b => b.AthleteId)
            .HasConstraintName("FK_Bans_Athletes");
    }
}