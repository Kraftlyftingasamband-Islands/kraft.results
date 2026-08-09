using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class RenameTeamToClub : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op: CLR type names changed (Team→Club, TeamId→ClubId) but all storage
            // names are pinned via HasColumnName/ToTable/HasConstraintName. This migration
            // exists solely to re-sync the model snapshot with the new CLR names.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: see Up(). Storage is unchanged; nothing to reverse.
        }
    }
}
