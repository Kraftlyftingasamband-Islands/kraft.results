using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KRAFT.Results.WebApi.Features.Attempts;

internal sealed class AttemptConfiguration : IEntityTypeConfiguration<Attempt>
{
    public void Configure(EntityTypeBuilder<Attempt> builder)
    {
        builder.ToTable("Attempts", "dbo");

        builder.HasIndex(e => e.ParticipationId, "_dta_index_Attempts_20_565577053__K2_1_3_4_5_6_7_8_9_10");

        builder.Property(e => e.CreatedBy)
            .HasMaxLength(50)
            .HasDefaultValue("klaus", "DF_Attempts_CreatedBy");

        builder.Property(e => e.CreatedOn)
            .HasDefaultValueSql("(getdate())", "DF_Attempts_CreatedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.ModifiedBy)
            .HasMaxLength(50)
            .HasDefaultValue("klaus", "DF_Attempts_ModifiedBy");

        builder.Property(e => e.ModifiedOn)
            .HasDefaultValueSql("(getdate())", "DF_Attempts_ModifiedOn")
            .HasColumnType("datetime");

        builder.Property(e => e.Weight)
            .HasColumnType("numeric(18, 2)");

        builder.HasOne(d => d.Participation).WithMany(p => p.Attempts)
            .HasForeignKey(d => d.ParticipationId)
            .HasConstraintName("FK_Attempts_Participations");
    }
}