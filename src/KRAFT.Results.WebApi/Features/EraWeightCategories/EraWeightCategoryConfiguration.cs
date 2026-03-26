using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.WebApi.Features.EraWeightCategories;

internal sealed class EraWeightCategoryConfiguration : IEntityTypeConfiguration<EraWeightCategory>
{
    public void Configure(EntityTypeBuilder<EraWeightCategory> builder)
    {
        builder.ToTable("EraWeightCategories", "dbo");

        builder.HasIndex(e => new { e.EraId, e.WeightCategoryId })
            .HasDatabaseName("IX_EraWeightCategories_EraId_WeightCategoryId");

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_EraWeightCategories_CreatedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.FromDate)
            .HasColumnType("datetime");

        builder.Property(e => e.ToDate)
            .HasColumnType("datetime");

        builder.HasOne(d => d.Era)
            .WithMany(p => p.EraWeightCategories)
            .HasForeignKey(d => d.EraId)
            .HasConstraintName("FK_EraWeightCategories_Eras");

        builder.HasOne(d => d.WeightCategory)
            .WithMany(p => p.EraWeightCategories)
            .HasForeignKey(d => d.WeightCategoryId)
            .HasConstraintName("FK_EraWeightCategories_WeightCategories");
    }
}