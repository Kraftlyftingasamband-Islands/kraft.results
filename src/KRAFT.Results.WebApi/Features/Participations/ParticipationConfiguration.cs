using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.WebApi.Features.Participations;

internal sealed class ParticipationConfiguration : IEntityTypeConfiguration<Participation>
{
    public void Configure(EntityTypeBuilder<Participation> builder)
    {
        builder.ToTable("Participations", "dbo");

        builder.HasIndex(e => e.MeetId, "_dta_index_Participations_20_75147313__K3_1_2_4_5_6_7_8_9_10_11_12_13_14_15_16_17_18");

        builder.HasIndex(e => e.ModifiedOn, "nci_wi_Participations_48291F9C-12F7-468C-BB5C-6AB9A431BCF6");

        builder.HasIndex(e => new { e.AthleteId, e.Disqualified }, "nci_wi_Participations_6F9B90B41FACB987C7A9");

        builder.Property(e => e.AgeCategoryId)
            .HasDefaultValue(1, "DF_Participations_AgeCategoryId");

        builder.Property(e => e.Benchpress)
            .HasColumnType("numeric(18, 2)");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(50)
            .HasDefaultValue("klaus", "DF_Participations_CreatedBy");

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_Participations_CreatedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.Deadlift)
            .HasColumnType("numeric(18, 2)");

        builder.Property(e => e.Ipfpoints)
            .HasColumnType("decimal(18, 2)")
            .HasColumnName("IPFPoints");

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(50)
            .HasDefaultValue("klaus", "DF_Participations_ModifiedBy");

        builder.Property(e => e.ModifiedOn)
            .HasDefaultValueSql("(getdate())", "DF_Participations_ModifiedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.Place)
            .HasDefaultValue(-1, "DF_Participations_Place");

        builder.Property(e => e.Squat)
            .HasColumnType("decimal(18, 2)");

        builder.Property(e => e.Total)
            .HasColumnType("decimal(18, 2)");

        builder.Property(e => e.Weight)
            .HasColumnType("decimal(18, 2)");

        builder.Property(e => e.Wilks)
            .HasColumnType("decimal(18, 2)");

        builder.HasOne(d => d.AgeCategory)
            .WithMany(p => p.Participations)
            .HasForeignKey(d => d.AgeCategoryId)
            .HasConstraintName("FK_Participations_AgeCategories");

        builder.HasOne(d => d.Athlete)
            .WithMany(p => p.Participations)
            .HasForeignKey(d => d.AthleteId)
            .HasConstraintName("FK_Participations_Athletes");

        builder.HasOne(d => d.Meet)
            .WithMany(p => p.Participations)
            .HasForeignKey(d => d.MeetId)
            .HasConstraintName("FK_Participations_Meets");

        builder.HasOne(d => d.Team)
            .WithMany(p => p.Participations)
            .HasForeignKey(d => d.TeamId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_Participations_Teams");

        builder.HasOne(d => d.WeightCategory)
            .WithMany(p => p.Participations)
            .HasForeignKey(d => d.WeightCategoryId)
            .HasConstraintName("FK_Participations_WeightCategories");
    }
}