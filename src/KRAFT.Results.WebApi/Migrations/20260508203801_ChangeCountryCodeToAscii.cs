using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class ChangeCountryCodeToAscii : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                schema: "dbo",
                table: "Teams",
                type: "char(3)",
                unicode: false,
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "ISL",
                oldClrType: typeof(string),
                oldType: "nchar(3)",
                oldFixedLength: true,
                oldMaxLength: 3,
                oldDefaultValue: "ISL")
                .Annotation("Relational:DefaultConstraintName", "DF_Teams_CountryCode")
                .OldAnnotation("Relational:DefaultConstraintName", "DF_Teams_CountryCode");

            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                schema: "dbo",
                table: "Athletes",
                type: "char(3)",
                unicode: false,
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "ISL",
                oldClrType: typeof(string),
                oldType: "nchar(3)",
                oldFixedLength: true,
                oldMaxLength: 3,
                oldDefaultValue: "ISL")
                .Annotation("Relational:DefaultConstraintName", "DF_Athletes_CountryCode")
                .OldAnnotation("Relational:DefaultConstraintName", "DF_Athletes_CountryCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                schema: "dbo",
                table: "Teams",
                type: "nchar(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "ISL",
                oldClrType: typeof(string),
                oldType: "char(3)",
                oldUnicode: false,
                oldFixedLength: true,
                oldMaxLength: 3,
                oldDefaultValue: "ISL")
                .Annotation("Relational:DefaultConstraintName", "DF_Teams_CountryCode")
                .OldAnnotation("Relational:DefaultConstraintName", "DF_Teams_CountryCode");

            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                schema: "dbo",
                table: "Athletes",
                type: "nchar(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                defaultValue: "ISL",
                oldClrType: typeof(string),
                oldType: "char(3)",
                oldUnicode: false,
                oldFixedLength: true,
                oldMaxLength: 3,
                oldDefaultValue: "ISL")
                .Annotation("Relational:DefaultConstraintName", "DF_Athletes_CountryCode")
                .OldAnnotation("Relational:DefaultConstraintName", "DF_Athletes_CountryCode");
        }
    }
}
