using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAthleteBansNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Bans_AthleteId",
                schema: "dbo",
                table: "Bans",
                column: "AthleteId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bans_Athletes",
                schema: "dbo",
                table: "Bans",
                column: "AthleteId",
                principalSchema: "dbo",
                principalTable: "Athletes",
                principalColumn: "AthleteId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bans_Athletes",
                schema: "dbo",
                table: "Bans");

            migrationBuilder.DropIndex(
                name: "IX_Bans_AthleteId",
                schema: "dbo",
                table: "Bans");
        }
    }
}
