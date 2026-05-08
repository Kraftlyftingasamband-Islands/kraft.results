using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceCountriesWithValueObject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add new CountryCode columns as NULLABLE on Athletes and Teams
            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                schema: "dbo",
                table: "Athletes",
                type: "nchar(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CountryCode",
                schema: "dbo",
                table: "Teams",
                type: "nchar(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: true);

            // Step 2: Backfill Athletes from Countries.ISO3
            migrationBuilder.Sql(
                "UPDATE a SET a.CountryCode = c.ISO3 FROM dbo.Athletes a INNER JOIN dbo.Countries c ON a.CountryId = c.CountryID");

            // Step 3: Backfill Teams from Countries.ISO3
            migrationBuilder.Sql(
                "UPDATE t SET t.CountryCode = c.ISO3 FROM dbo.Teams t INNER JOIN dbo.Countries c ON t.CountryId = c.CountryID");

            // Step 4: Fix TeamId 32 (had NULL CountryId, not in Countries table)
            migrationBuilder.Sql(
                "UPDATE dbo.Teams SET CountryCode = 'ISL' WHERE TeamId = 32 AND CountryCode IS NULL");

            // Step 5: Make CountryCode NOT NULL on both tables
            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                schema: "dbo",
                table: "Athletes",
                type: "nchar(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nchar(3)",
                oldFixedLength: true,
                oldMaxLength: 3,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CountryCode",
                schema: "dbo",
                table: "Teams",
                type: "nchar(3)",
                fixedLength: true,
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nchar(3)",
                oldFixedLength: true,
                oldMaxLength: 3,
                oldNullable: true);

            // Step 6: Add default constraints for CountryCode = 'ISL'
            migrationBuilder.Sql(
                "ALTER TABLE dbo.Athletes ADD CONSTRAINT DF_Athletes_CountryCode DEFAULT N'ISL' FOR CountryCode");

            migrationBuilder.Sql(
                "ALTER TABLE dbo.Teams ADD CONSTRAINT DF_Teams_CountryCode DEFAULT N'ISL' FOR CountryCode");

            // Step 7: Drop FK constraints
            migrationBuilder.DropForeignKey(
                name: "FK_Athletes_Countries",
                schema: "dbo",
                table: "Athletes");

            migrationBuilder.DropForeignKey(
                name: "FK_Teams_Countries",
                schema: "dbo",
                table: "Teams");

            // Step 8: Drop indexes that depend on CountryId (must precede column drop)
            migrationBuilder.DropIndex(
                name: "_dta_index_Athletes_20_971150505__K8_K1",
                schema: "dbo",
                table: "Athletes");

            migrationBuilder.DropIndex(
                name: "IX_Teams_CountryId",
                schema: "dbo",
                table: "Teams");

            // Step 9: Drop old CountryId columns
            migrationBuilder.DropColumn(
                name: "CountryId",
                schema: "dbo",
                table: "Athletes")
                .Annotation("Relational:DefaultConstraintName", "DF_Athletes_CountryId");

            migrationBuilder.DropColumn(
                name: "CountryId",
                schema: "dbo",
                table: "Teams");

            // Step 10: Drop the Countries table
            migrationBuilder.DropTable(
                name: "Countries",
                schema: "dbo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Reverting the Countries table migration is not supported.");
        }
    }
}
