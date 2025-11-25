using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.Core.Features.AthleteAliases;

internal sealed class AthleteAliasConfiguration : IEntityTypeConfiguration<AthleteAlias>
{
    public void Configure(EntityTypeBuilder<AthleteAlias> builder)
    {
        builder.ToTable("AthleteAliases", "dbo");

        builder.Property(e => e.Alias)
            .HasMaxLength(100);

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_AthleteAliases_CreatedOn")
            .HasColumnType("datetime");
    }
}