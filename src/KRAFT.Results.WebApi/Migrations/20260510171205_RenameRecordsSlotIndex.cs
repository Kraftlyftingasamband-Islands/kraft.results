using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class RenameRecordsSlotIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "nci_wi_Records_40FB705E-F1AE-4E31-998B-DE4A0332DA61",
                schema: "dbo",
                table: "Records");

            migrationBuilder.CreateIndex(
                name: "IX_Records_AgeCategoryId",
                schema: "dbo",
                table: "Records",
                column: "AgeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Records_Slot",
                schema: "dbo",
                table: "Records",
                columns: new[] { "EraId", "AgeCategoryId", "WeightCategoryId", "RecordCategoryId", "IsRaw", "IsStandard", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Records_AgeCategoryId",
                schema: "dbo",
                table: "Records");

            migrationBuilder.DropIndex(
                name: "IX_Records_Slot",
                schema: "dbo",
                table: "Records");

            migrationBuilder.CreateIndex(
                name: "nci_wi_Records_40FB705E-F1AE-4E31-998B-DE4A0332DA61",
                schema: "dbo",
                table: "Records",
                columns: new[] { "AgeCategoryId", "EraId", "RecordCategoryId", "WeightCategoryId", "IsRaw", "Date" });
        }
    }
}
