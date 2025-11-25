using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.WebApi.Features.MeetTypes;

internal sealed class MeetTypeConfiguration : IEntityTypeConfiguration<MeetType>
{
    public void Configure(EntityTypeBuilder<MeetType> builder)
    {
        builder.ToTable("MeetTypes", "dbo");

        builder.Property(e => e.MeetTypeId)
            .ValueGeneratedNever();

        builder.Property(e => e.Title)
            .HasMaxLength(50);
    }
}