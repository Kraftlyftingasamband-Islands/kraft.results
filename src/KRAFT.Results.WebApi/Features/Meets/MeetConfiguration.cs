using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.WebApi.Features.Meets;

internal sealed class MeetConfiguration : IEntityTypeConfiguration<Meet>
{
    public void Configure(EntityTypeBuilder<Meet> builder)
    {
        builder.ToTable("Meets", "dbo");

        builder.HasIndex(e => e.Slug, "IX_Meets_Slug_Unique");

        builder.HasIndex(e => e.PublishedResults, "nci_wi_Meets_096AE4AB-BAC9-480E-99C7-F2591AA73EF9");

        builder.HasIndex(e => new { e.IsRaw, e.MeetTypeId }, "nci_wi_Meets_B4C6AEB59052E88F812BB1BBA9EB448F");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(50)
            .HasDefaultValue("klaus", "DF_Meets_CreatedBy");

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_Meets_CreatedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.EndDate)
            .HasColumnType("datetime");

        builder.Property(e => e.Location)
            .HasMaxLength(50)
            .HasDefaultValue(string.Empty, "DF_Meets_Location");

        builder.Property(e => e.MeetTypeId)
            .HasDefaultValue(1, "DF_Meets_MeetTypeId");

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(50)
            .HasDefaultValue("klaus", "DF_Meets_ModifiedBy");

        builder.Property(e => e.ModifiedOn)
            .HasDefaultValueSql("(getdate())", "DF_Meets_ModifiedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.PublishedInCalendar)
            .HasDefaultValue(true, "DF_Meets_PublishedInCalendar");

        builder.Property(e => e.PublishedResults)
            .HasDefaultValue(true, "DF_Meets_Published");

        builder.Property(e => e.RecordsPossible)
            .HasDefaultValue(true, "DF_Meets_RecordsPossible");

        builder.Property(e => e.ResultModeId)
            .HasDefaultValue(1, "DF_Meets_ResultModeId");

        builder.Property(e => e.ShowBodyWeight)
            .HasDefaultValue(true, "DF_Meets_ShowBodyWeight");

        builder.Property(e => e.ShowTeamPoints)
            .HasDefaultValue(true, "DF_Meets_ShowTeamPoints");

        builder.Property(e => e.ShowWilks)
            .HasDefaultValue(true, "DF_Meets_ShowWilks");

        builder.Property(e => e.Slug)
            .HasMaxLength(100);

        builder.Property(e => e.StartDate)
            .HasColumnType("datetime");

        builder.Property(e => e.Text)
            .HasDefaultValue(string.Empty, "DF_Meets_Text");

        builder.Property(e => e.Title)
            .HasMaxLength(100);

        builder.HasOne(d => d.MeetType).WithMany(p => p.Meets)
            .HasForeignKey(d => d.MeetTypeId)
            .HasConstraintName("FK_Meets_MeetTypes");
    }
}