using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.Core.Ads;

internal sealed class AdConfiguration : IEntityTypeConfiguration<Ad>
{
    public void Configure(EntityTypeBuilder<Ad> builder)
    {
        builder.ToTable("Ads", "dbo");

        builder.Property(e => e.AdSlotId)
            .HasMaxLength(50)
            .IsUnicode(false);

        builder.Property(e => e.ClickUrl)
            .HasMaxLength(500)
            .IsUnicode(false);

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(50)
            .IsUnicode(false);

        builder.Property(e => e.CreatedOn)
            .HasPrecision(0)
            .HasDefaultValueSql("(getutcdate())", "DF_Ads_CreatedOn");

        builder.Property(e => e.Enabled)
            .HasDefaultValue(true, "DF_Ads_Enabled");

        builder.Property(e => e.FromDate)
            .HasPrecision(0);

        builder.Property(e => e.ImageUrl)
            .HasMaxLength(500)
            .IsUnicode(false);

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(50)
            .IsUnicode(false);

        builder.Property(e => e.ModifiedOn)
            .HasPrecision(0)
            .HasDefaultValueSql("(getutcdate())", "DF_Ads_ModifiedOn");

        builder.Property(e => e.ToDate)
            .HasPrecision(0);
    }
}