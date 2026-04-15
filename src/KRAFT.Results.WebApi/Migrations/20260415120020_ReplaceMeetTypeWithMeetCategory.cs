using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceMeetTypeWithMeetCategory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Meets_MeetTypes",
                schema: "dbo",
                table: "Meets");

            migrationBuilder.DropTable(
                name: "MeetTypes",
                schema: "dbo");

            migrationBuilder.RenameIndex(
                name: "IX_Meets_MeetTypeId",
                schema: "dbo",
                table: "Meets",
                newName: "IX_Meets_Category");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Meets_Category",
                schema: "dbo",
                table: "Meets",
                newName: "IX_Meets_MeetTypeId");

            migrationBuilder.CreateTable(
                name: "MeetTypes",
                schema: "dbo",
                columns: table => new
                {
                    MeetTypeId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MeetTypes", x => x.MeetTypeId);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [dbo].[MeetTypes] (MeetTypeId, Title) VALUES
                    (1, 'Powerlifting'),
                    (2, 'Benchpress'),
                    (3, 'Deadlift'),
                    (4, 'Squat'),
                    (5, 'PushPull');
                """);

            migrationBuilder.AddForeignKey(
                name: "FK_Meets_MeetTypes",
                schema: "dbo",
                table: "Meets",
                column: "MeetTypeId",
                principalSchema: "dbo",
                principalTable: "MeetTypes",
                principalColumn: "MeetTypeId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
