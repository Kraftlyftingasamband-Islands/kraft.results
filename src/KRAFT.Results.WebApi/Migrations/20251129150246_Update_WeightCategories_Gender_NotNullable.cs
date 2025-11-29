using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class Update_WeightCategories_Gender_NotNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Gender",
                schema: "dbo",
                table: "WeightCategories",
                type: "varchar(1)",
                unicode: false,
                maxLength: 1,
                nullable: false,
                defaultValue: "m",
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldUnicode: false,
                oldMaxLength: 1,
                oldNullable: true,
                oldDefaultValue: "M")
                .Annotation("Relational:DefaultConstraintName", "DF_WeightCategories_Gender")
                .OldAnnotation("Relational:DefaultConstraintName", "DF_WeightCategories_Gender");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Gender",
                schema: "dbo",
                table: "WeightCategories",
                type: "varchar(1)",
                unicode: false,
                maxLength: 1,
                nullable: true,
                defaultValue: "M",
                oldClrType: typeof(string),
                oldType: "varchar(1)",
                oldUnicode: false,
                oldMaxLength: 1,
                oldDefaultValue: "m")
                .Annotation("Relational:DefaultConstraintName", "DF_WeightCategories_Gender")
                .OldAnnotation("Relational:DefaultConstraintName", "DF_WeightCategories_Gender");
        }
    }
}
