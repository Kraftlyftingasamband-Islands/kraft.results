using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.Core.AgeCategories;

internal sealed class AgeCategoryConfiguration : IEntityTypeConfiguration<AgeCategory>
{
    public void Configure(EntityTypeBuilder<AgeCategory> builder)
    {
        builder.ToTable("AgeCategories", "dbo");

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_AgeCategories_CreatedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.Slug)
            .HasMaxLength(50);

        builder.Property(e => e.Title)
            .HasMaxLength(50);

        builder.Property(e => e.TitleShort)
            .HasMaxLength(5);
    }
}