using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.Core.AdSlots;

internal sealed class AdSlotConfiguration : IEntityTypeConfiguration<AdSlot>
{
    public void Configure(EntityTypeBuilder<AdSlot> builder)
    {
        builder.ToTable("AdSlots", "dbo");

        builder.Property(e => e.Id)
            .HasMaxLength(50)
            .IsUnicode(false);

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_AdSlots_CreatedOn");

        builder.Property(e => e.Description)
            .HasMaxLength(1000)
            .IsUnicode(false);

        builder.Property(e => e.Enabled)
            .HasDefaultValue(true, "DF_AdSlots_Enabled");

        builder.Property(e => e.ModifiedOn)
            .HasDefaultValueSql("(getdate())", "DF_AdSlots_ModifiedOn");
    }
}