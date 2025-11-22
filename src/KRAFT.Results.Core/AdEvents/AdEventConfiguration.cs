using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.Core.AdEvents;

internal sealed class AdEventConfiguration : IEntityTypeConfiguration<AdEvent>
{
    public void Configure(EntityTypeBuilder<AdEvent> builder)
    {
        builder.ToTable("AdEvents", "dbo");

        builder.Property(e => e.Ip)
            .HasMaxLength(20)
            .IsUnicode(false)
            .HasColumnName("IP");
    }
}