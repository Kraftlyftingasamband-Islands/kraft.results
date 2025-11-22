using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.Core.WeightCategories;

internal sealed class WeightCategoryConfiguration : IEntityTypeConfiguration<WeightCategory>
{
    public void Configure(EntityTypeBuilder<WeightCategory> builder)
    {
        builder.ToTable("WeightCategories", "dbo");

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_WeightCategories_CreatedOn_1")
            .HasColumnType("datetime");

        builder.Property(e => e.Gender)
            .HasMaxLength(1)
            .IsUnicode(false)
            .HasDefaultValue("M", "DF_WeightCategories_Gender");

        builder.Property(e => e.MaxWeight)
            .HasColumnType("numeric(18, 2)");

        builder.Property(e => e.MinWeight)
            .HasColumnType("numeric(18, 2)");

        builder.Property(e => e.Slug)
            .HasMaxLength(20);

        builder.Property(e => e.Title)
            .HasMaxLength(50);
    }
}