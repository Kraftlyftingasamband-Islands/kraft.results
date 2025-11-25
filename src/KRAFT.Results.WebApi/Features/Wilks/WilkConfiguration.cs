using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.WebApi.Features.Wilks;

internal sealed class WilkConfiguration : IEntityTypeConfiguration<Wilk>
{
    public void Configure(EntityTypeBuilder<Wilk> builder)
    {
        builder.ToTable("Wilks", "dbo");

        builder.Property(e => e.Coefficient)
            .HasColumnType("decimal(18, 4)");

        builder.Property(e => e.Gender)
            .HasMaxLength(1)
            .IsUnicode(false)
            .IsFixedLength();

        builder.Property(e => e.Weight)
            .HasColumnType("decimal(18, 1)");
    }
}