using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.Core.Photos;

internal sealed class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.ToTable("Photos", "dbo");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(50);

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_Photos_CreatedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.Date)
            .HasColumnType("datetime");

        builder.Property(e => e.ImageFilname)
            .HasMaxLength(200);

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(50);

        builder.Property(e => e.ModifiedOn)
            .HasColumnType("datetime");

        builder.Property(e => e.Photographer)
            .HasMaxLength(100);

        builder.HasOne(d => d.Meet).WithMany(p => p.Photos)
            .HasForeignKey(d => d.MeetId)
            .HasConstraintName("FK_Photos_Meets");
    }
}