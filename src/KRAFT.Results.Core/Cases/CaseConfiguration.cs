using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.Core.Cases;

internal sealed class CaseConfiguration : IEntityTypeConfiguration<Case>
{
    public void Configure(EntityTypeBuilder<Case> builder)
    {
        builder.HasKey(e => e.CaseId).HasName("PK_Reports");

        builder.ToTable("Cases", "dbo");

        builder.Property(e => e.ClosedOn)
            .HasColumnType("datetime");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(50)
            .HasDefaultValueSql("(getdate())", "DF_Reports_CreatedBy");

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_Reports_CreatedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.FromEmail)
            .HasMaxLength(100);

        builder.Property(e => e.FromName)
            .HasMaxLength(100);

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(50);

        builder.Property(e => e.ModifiedOn)
            .HasColumnType("datetime");

        builder.Property(e => e.Url)
            .HasMaxLength(500);
    }
}