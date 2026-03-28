using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRecordStatusAndAuditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                schema: "dbo",
                table: "Records",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedOn",
                schema: "dbo",
                table: "Records",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                schema: "dbo",
                table: "Records",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<byte>(
                name: "Status",
                schema: "dbo",
                table: "Records",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.Sql("UPDATE dbo.Records SET Status = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                schema: "dbo",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "ModifiedOn",
                schema: "dbo",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                schema: "dbo",
                table: "Records");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "dbo",
                table: "Records");
        }
    }
}
