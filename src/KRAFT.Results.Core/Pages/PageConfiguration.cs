using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.Core.Pages;

internal sealed class PageConfiguration : IEntityTypeConfiguration<Page>
{
    public void Configure(EntityTypeBuilder<Page> builder)
    {
        builder.ToTable("Pages", "dbo");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(50)
            .HasDefaultValue("klaus", "DF_Pages_CreatedBy");

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_Pages_CreatedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(50)
            .HasDefaultValue("klaus", "DF_Pages_ModifiedBy");

        builder.Property(e => e.ModifiedOn)
            .HasDefaultValueSql("(getdate())", "DF_Pages_ModifiedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.Title)
            .HasMaxLength(200);

        builder.HasOne(d => d.PageGroup).WithMany(p => p.Pages)
            .HasForeignKey(d => d.PageGroupId)
            .HasConstraintName("FK_Pages_PageGroups");
    }
}