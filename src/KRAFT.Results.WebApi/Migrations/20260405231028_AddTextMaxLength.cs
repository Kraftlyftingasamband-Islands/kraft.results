using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddTextMaxLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Text",
                schema: "dbo",
                table: "Meets",
                type: "nvarchar(max)",
                maxLength: 8000,
                nullable: true,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true,
                oldDefaultValue: "")
                .Annotation("Relational:DefaultConstraintName", "DF_Meets_Text")
                .OldAnnotation("Relational:DefaultConstraintName", "DF_Meets_Text");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Text",
                schema: "dbo",
                table: "Meets",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 8000,
                oldNullable: true,
                oldDefaultValue: "")
                .Annotation("Relational:DefaultConstraintName", "DF_Meets_Text")
                .OldAnnotation("Relational:DefaultConstraintName", "DF_Meets_Text");
        }
    }
}
