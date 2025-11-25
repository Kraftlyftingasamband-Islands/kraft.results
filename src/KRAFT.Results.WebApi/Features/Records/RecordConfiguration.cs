using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.WebApi.Features.Records;

internal sealed class RecordConfiguration : IEntityTypeConfiguration<Record>
{
    public void Configure(EntityTypeBuilder<Record> builder)
    {
        builder.ToTable("Records", "dbo");

        builder.HasIndex(e => new { e.IsCurrent, e.EraId }, "nci_wi_Records_3CB8ADEAD69A6DA29B4DC80D395ABC87");

        builder.HasIndex(e => new { e.AgeCategoryId, e.EraId, e.RecordCategoryId, e.WeightCategoryId, e.IsRaw, e.Date }, "nci_wi_Records_40FB705E-F1AE-4E31-998B-DE4A0332DA61");

        builder.HasIndex(e => new { e.AttemptId, e.EraId, e.IsCurrent }, "nci_wi_Records_FCB2C18B-AF9A-44EE-BAE9-051E384EC259");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(50);

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_Records_CreatedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.Weight)
            .HasColumnType("decimal(18, 2)");

        builder.HasOne(d => d.AgeCategory)
            .WithMany(p => p.Records)
            .HasForeignKey(d => d.AgeCategoryId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Records_AgeCategories");

        builder.HasOne(d => d.Attempt)
            .WithMany(p => p.Records)
            .HasForeignKey(d => d.AttemptId)
            .HasConstraintName("FK_Records_Attempts");

        builder.HasOne(d => d.Era)
            .WithMany(p => p.Records)
            .HasForeignKey(d => d.EraId)
            .HasConstraintName("FK_Records_Eras");

        builder.HasOne(d => d.WeightCategory)
            .WithMany(p => p.Records)
            .HasForeignKey(d => d.WeightCategoryId)
            .OnDelete(DeleteBehavior.ClientSetNull)
            .HasConstraintName("FK_Records_WeightCategories");
    }
}