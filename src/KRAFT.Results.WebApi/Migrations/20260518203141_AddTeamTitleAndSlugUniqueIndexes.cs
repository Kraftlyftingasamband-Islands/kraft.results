using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamTitleAndSlugUniqueIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Teams_Slug_Unique",
                schema: "dbo",
                table: "Teams",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_Title_Unique",
                schema: "dbo",
                table: "Teams",
                column: "Title",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Teams_Slug_Unique",
                schema: "dbo",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Teams_Title_Unique",
                schema: "dbo",
                table: "Teams");
        }
    }
}
