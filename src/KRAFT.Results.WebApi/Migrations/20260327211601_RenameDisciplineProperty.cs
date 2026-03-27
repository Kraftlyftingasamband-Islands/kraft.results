using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class RenameDisciplineProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No schema changes — property rename only (DisciplineId -> Discipline) with HasColumnName mapping
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No schema changes to revert
        }
    }
}
