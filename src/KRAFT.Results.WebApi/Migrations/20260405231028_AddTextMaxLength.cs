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
                type: "nvarchar(4000)",
                maxLength: 4000,
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
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000,
                oldNullable: true,
                oldDefaultValue: "")
                .Annotation("Relational:DefaultConstraintName", "DF_Meets_Text")
                .OldAnnotation("Relational:DefaultConstraintName", "DF_Meets_Text");
        }
    }
}
