using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.Core.Teams;

internal sealed class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("Teams", "dbo");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(50)
            .HasDefaultValue("klaus", "DF_Teams_CreatedBy");

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_Teams_CreatedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.LogoImageFilename)
            .HasMaxLength(200);

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(50)
            .HasDefaultValue("klaus", "DF_Teams_ModifiedBy");

        builder.Property(e => e.ModifiedOn)
            .HasDefaultValueSql("(getdate())", "DF_Teams_ModifiedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.Slug)
            .HasMaxLength(50);

        builder.Property(e => e.Title)
            .HasMaxLength(50);

        builder.Property(e => e.TitleFull)
            .HasMaxLength(100)
            .HasDefaultValue(string.Empty, "DF_Teams_FullTitle");

        builder.Property(e => e.TitleShort)
            .HasMaxLength(3);

        builder.HasOne(d => d.Country)
            .WithMany(p => p.Teams)
            .HasForeignKey(d => d.CountryId)
            .HasConstraintName("FK_Teams_Countries");
    }
}