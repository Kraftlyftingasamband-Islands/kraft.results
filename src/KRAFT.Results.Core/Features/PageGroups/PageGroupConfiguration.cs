using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.Core.Features.PageGroups;

internal sealed class PageGroupConfiguration : IEntityTypeConfiguration<PageGroup>
{
    public void Configure(EntityTypeBuilder<PageGroup> builder)
    {
        builder.ToTable("PageGroups", "dbo");

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_PageGroups_CreatedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.Title)
            .HasMaxLength(50);
    }
}