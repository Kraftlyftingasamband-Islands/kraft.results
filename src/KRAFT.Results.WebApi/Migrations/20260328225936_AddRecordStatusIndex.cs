using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordStatusIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Records_Status_Composite",
                schema: "dbo",
                table: "Records",
                columns: new[] { "Status", "EraId", "AgeCategoryId", "WeightCategoryId", "RecordCategoryId", "IsRaw" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Records_Status_Composite",
                schema: "dbo",
                table: "Records");
        }
    }
}
