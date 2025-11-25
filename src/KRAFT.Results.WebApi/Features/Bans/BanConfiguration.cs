using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.WebApi.Features.Bans;

internal sealed class BanConfiguration : IEntityTypeConfiguration<Ban>
{
    public void Configure(EntityTypeBuilder<Ban> builder)
    {
        builder.ToTable("Bans", "dbo");

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getutcdate())", "DF_Bansd_CreatedOn");

        builder.Property(e => e.FromDate)
            .HasColumnType("datetime");

        builder.Property(e => e.ToDate)
            .HasColumnType("datetime");
    }
}