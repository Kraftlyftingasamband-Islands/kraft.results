using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddEraWeightCategoryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EraWeightCategories_EraId",
                schema: "dbo",
                table: "EraWeightCategories");

            migrationBuilder.CreateIndex(
                name: "IX_EraWeightCategories_EraId_WeightCategoryId",
                schema: "dbo",
                table: "EraWeightCategories",
                columns: new[] { "EraId", "WeightCategoryId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EraWeightCategories_EraId_WeightCategoryId",
                schema: "dbo",
                table: "EraWeightCategories");

            migrationBuilder.CreateIndex(
                name: "IX_EraWeightCategories_EraId",
                schema: "dbo",
                table: "EraWeightCategories",
                column: "EraId");
        }
    }
}
