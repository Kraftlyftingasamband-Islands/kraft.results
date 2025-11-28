using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class Update_Athletes_Slug_NotNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Athletes_Slug_Unique",
                schema: "dbo",
                table: "Athletes");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "dbo",
                table: "Athletes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_Slug_Unique",
                schema: "dbo",
                table: "Athletes",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Athletes_Slug_Unique",
                schema: "dbo",
                table: "Athletes");

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                schema: "dbo",
                table: "Athletes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_Slug_Unique",
                schema: "dbo",
                table: "Athletes",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");
        }
    }
}
