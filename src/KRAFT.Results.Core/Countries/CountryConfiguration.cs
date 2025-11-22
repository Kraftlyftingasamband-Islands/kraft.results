using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.Core.Countries;

internal sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.HasKey(e => e.CountryId).HasName("PK_Country");

        builder.ToTable("Countries", "dbo");

        builder.Property(e => e.CountryId)
            .ValueGeneratedNever()
            .HasColumnName("CountryID");

        builder.Property(e => e.Iso2)
            .HasMaxLength(2)
            .IsFixedLength()
            .HasColumnName("ISO2");

        builder.Property(e => e.Iso3)
            .HasMaxLength(3)
            .IsFixedLength()
            .HasColumnName("ISO3");

        builder.Property(e => e.Name)
            .HasMaxLength(50);
    }
}