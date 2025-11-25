using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.Core.Features.Eras;

internal sealed class EraConfiguration : IEntityTypeConfiguration<Era>
{
    public void Configure(EntityTypeBuilder<Era> builder)
    {
        builder.ToTable("Eras", "dbo");

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_Eras_CreatedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.Slug)
            .HasMaxLength(50);

        builder.Property(e => e.Title)
            .HasMaxLength(50);
    }
}