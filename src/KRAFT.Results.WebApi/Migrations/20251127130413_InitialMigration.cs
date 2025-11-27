using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.CreateTable(
                name: "AgeCategories",
                schema: "dbo",
                columns: table => new
                {
                    AgeCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TitleShort = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_AgeCategories_CreatedOn"),
                    Slug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgeCategories", x => x.AgeCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "AthleteAliases",
                schema: "dbo",
                columns: table => new
                {
                    AthleteAliasId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AthleteId = table.Column<int>(type: "int", nullable: false),
                    Alias = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_AthleteAliases_CreatedOn")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AthleteAliases", x => x.AthleteAliasId);
                });

            migrationBuilder.CreateTable(
                name: "Bans",
                schema: "dbo",
                columns: table => new
                {
                    BanId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AthleteId = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "(getutcdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Bansd_CreatedOn")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bans", x => x.BanId);
                });

            migrationBuilder.CreateTable(
                name: "Cases",
                schema: "dbo",
                columns: table => new
                {
                    CaseId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromEmail = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FromName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ClosedOn = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Reports_CreatedOn"),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Reports_CreatedBy"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reports", x => x.CaseId);
                });

            migrationBuilder.CreateTable(
                name: "Countries",
                schema: "dbo",
                columns: table => new
                {
                    CountryID = table.Column<int>(type: "int", nullable: false),
                    ISO2 = table.Column<string>(type: "nchar(2)", fixedLength: true, maxLength: 2, nullable: false),
                    ISO3 = table.Column<string>(type: "nchar(3)", fixedLength: true, maxLength: 3, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Country", x => x.CountryID);
                });

            migrationBuilder.CreateTable(
                name: "Eras",
                schema: "dbo",
                columns: table => new
                {
                    EraId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Eras_CreatedOn"),
                    Slug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eras", x => x.EraId);
                });

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

            migrationBuilder.CreateTable(
                name: "PageGroups",
                schema: "dbo",
                columns: table => new
                {
                    PageGroupId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_PageGroups_CreatedOn")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageGroups", x => x.PageGroupId);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "dbo",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Roles_CreatedOn")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "dbo",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Password = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Firstname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Lastname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Users_Created"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Users_Modified"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_Users_ModifiedBy"),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_Users_CreatedBy"),
                    FacebookUserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "WeightCategories",
                schema: "dbo",
                columns: table => new
                {
                    WeightCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MinWeight = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    MaxWeight = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Gender = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: true, defaultValue: "M")
                        .Annotation("Relational:DefaultConstraintName", "DF_WeightCategories_Gender"),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_WeightCategories_CreatedOn_1"),
                    JuniorsOnly = table.Column<bool>(type: "bit", nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightCategories", x => x.WeightCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "Wilks",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Weight = table.Column<decimal>(type: "decimal(18,1)", nullable: false),
                    Coefficient = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Gender = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wilks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Teams",
                schema: "dbo",
                columns: table => new
                {
                    TeamId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TitleShort = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    LogoImageFilename = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Slug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TitleFull = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "")
                        .Annotation("Relational:DefaultConstraintName", "DF_Teams_FullTitle"),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Teams_CreatedOn"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Teams_ModifiedOn"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_Teams_ModifiedBy"),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_Teams_CreatedBy")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.TeamId);
                    table.ForeignKey(
                        name: "FK_Teams_Countries",
                        column: x => x.CountryId,
                        principalSchema: "dbo",
                        principalTable: "Countries",
                        principalColumn: "CountryID");
                });

            migrationBuilder.CreateTable(
                name: "Meets",
                schema: "dbo",
                columns: table => new
                {
                    MeetId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    MeetTypeId = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                        .Annotation("Relational:DefaultConstraintName", "DF_Meets_MeetTypeId"),
                    CalcPlaces = table.Column<bool>(type: "bit", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true, defaultValue: "")
                        .Annotation("Relational:DefaultConstraintName", "DF_Meets_Text"),
                    Location = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true, defaultValue: "")
                        .Annotation("Relational:DefaultConstraintName", "DF_Meets_Location"),
                    PublishedResults = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_Meets_Published"),
                    ResultModeId = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                        .Annotation("Relational:DefaultConstraintName", "DF_Meets_ResultModeId"),
                    PublishedInCalendar = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_Meets_PublishedInCalendar"),
                    IsInTeamCompetition = table.Column<bool>(type: "bit", nullable: false),
                    ShowWilks = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_Meets_ShowWilks"),
                    ShowTeamPoints = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_Meets_ShowTeamPoints"),
                    ShowBodyWeight = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_Meets_ShowBodyWeight"),
                    ShowTeams = table.Column<bool>(type: "bit", nullable: false),
                    RecordsPossible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                        .Annotation("Relational:DefaultConstraintName", "DF_Meets_RecordsPossible"),
                    IsRaw = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Meets_CreatedOn"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Meets_ModifiedOn"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_Meets_ModifiedBy"),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_Meets_CreatedBy")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Meets", x => x.MeetId);
                    table.ForeignKey(
                        name: "FK_Meets_MeetTypes",
                        column: x => x.MeetTypeId,
                        principalSchema: "dbo",
                        principalTable: "MeetTypes",
                        principalColumn: "MeetTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pages",
                schema: "dbo",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PageGroupId = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Pages_CreatedOn"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Pages_ModifiedOn"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_Pages_ModifiedBy"),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_Pages_CreatedBy")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.PageId);
                    table.ForeignKey(
                        name: "FK_Pages_PageGroups",
                        column: x => x.PageGroupId,
                        principalSchema: "dbo",
                        principalTable: "PageGroups",
                        principalColumn: "PageGroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "dbo",
                columns: table => new
                {
                    UserRoleId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_UserRoles_CreatedOn"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_UserRoles_ModifiedOn"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_UserRoles_ModifiedBy"),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_UserRoles_CreatedBy")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.UserRoleId);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles",
                        column: x => x.RoleId,
                        principalSchema: "dbo",
                        principalTable: "Roles",
                        principalColumn: "RoleId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users",
                        column: x => x.UserId,
                        principalSchema: "dbo",
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EraWeightCategories",
                schema: "dbo",
                columns: table => new
                {
                    EraWeightCategoryId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EraId = table.Column<int>(type: "int", nullable: false),
                    WeightCategoryId = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ToDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_EraWeightCategories_CreatedOn")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EraWeightCategories", x => x.EraWeightCategoryId);
                    table.ForeignKey(
                        name: "FK_EraWeightCategories_Eras",
                        column: x => x.EraId,
                        principalSchema: "dbo",
                        principalTable: "Eras",
                        principalColumn: "EraId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EraWeightCategories_WeightCategories",
                        column: x => x.WeightCategoryId,
                        principalSchema: "dbo",
                        principalTable: "WeightCategories",
                        principalColumn: "WeightCategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Athletes",
                schema: "dbo",
                columns: table => new
                {
                    AthleteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Firstname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Lastname = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "varchar(1)", unicode: false, maxLength: 1, nullable: false, defaultValue: "m")
                        .Annotation("Relational:DefaultConstraintName", "DF_Athletes_Gender"),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Athletes_CreatedOn"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Athletes_ModifiedOn"),
                    ProfileImageFilename = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_Athletes_ModifiedBy"),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_Athletes_CreatedBy"),
                    CountryId = table.Column<int>(type: "int", nullable: false, defaultValue: 352)
                        .Annotation("Relational:DefaultConstraintName", "DF_Athletes_CountryId"),
                    TeamId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Athletes", x => x.AthleteId);
                    table.ForeignKey(
                        name: "FK_Athletes_Countries",
                        column: x => x.CountryId,
                        principalSchema: "dbo",
                        principalTable: "Countries",
                        principalColumn: "CountryID");
                    table.ForeignKey(
                        name: "FK_Athletes_Teams",
                        column: x => x.TeamId,
                        principalSchema: "dbo",
                        principalTable: "Teams",
                        principalColumn: "TeamId");
                });

            migrationBuilder.CreateTable(
                name: "Photos",
                schema: "dbo",
                columns: table => new
                {
                    PhotoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MeetId = table.Column<int>(type: "int", nullable: true),
                    Photographer = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Date = table.Column<DateTime>(type: "datetime", nullable: false),
                    ImageFilname = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Photos_CreatedOn"),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ModifiedOn = table.Column<DateTime>(type: "datetime", nullable: false),
                    ModifiedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.PhotoId);
                    table.ForeignKey(
                        name: "FK_Photos_Meets",
                        column: x => x.MeetId,
                        principalSchema: "dbo",
                        principalTable: "Meets",
                        principalColumn: "MeetId");
                });

            migrationBuilder.CreateTable(
                name: "Participations",
                schema: "dbo",
                columns: table => new
                {
                    ParticipationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AthleteId = table.Column<int>(type: "int", nullable: false),
                    MeetId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WeightCategoryId = table.Column<int>(type: "int", nullable: false),
                    TeamId = table.Column<int>(type: "int", nullable: true),
                    AgeCategoryId = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                        .Annotation("Relational:DefaultConstraintName", "DF_Participations_AgeCategoryId"),
                    Place = table.Column<int>(type: "int", nullable: false, defaultValue: -1)
                        .Annotation("Relational:DefaultConstraintName", "DF_Participations_Place"),
                    Disqualified = table.Column<bool>(type: "bit", nullable: false),
                    Squat = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Benchpress = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Deadlift = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Wilks = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IPFPoints = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Participations_CreatedOn"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Participations_ModifiedOn"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_Participations_ModifiedBy"),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_Participations_CreatedBy"),
                    LotNo = table.Column<int>(type: "int", nullable: false),
                    TeamPoints = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participations", x => x.ParticipationId);
                    table.ForeignKey(
                        name: "FK_Participations_AgeCategories",
                        column: x => x.AgeCategoryId,
                        principalSchema: "dbo",
                        principalTable: "AgeCategories",
                        principalColumn: "AgeCategoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Participations_Athletes",
                        column: x => x.AthleteId,
                        principalSchema: "dbo",
                        principalTable: "Athletes",
                        principalColumn: "AthleteId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Participations_Meets",
                        column: x => x.MeetId,
                        principalSchema: "dbo",
                        principalTable: "Meets",
                        principalColumn: "MeetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Participations_Teams",
                        column: x => x.TeamId,
                        principalSchema: "dbo",
                        principalTable: "Teams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Participations_WeightCategories",
                        column: x => x.WeightCategoryId,
                        principalSchema: "dbo",
                        principalTable: "WeightCategories",
                        principalColumn: "WeightCategoryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Attempts",
                schema: "dbo",
                columns: table => new
                {
                    AttemptId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParticipationId = table.Column<int>(type: "int", nullable: false),
                    DisciplineId = table.Column<byte>(type: "tinyint", nullable: false),
                    Round = table.Column<short>(type: "smallint", nullable: false),
                    Weight = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Good = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Attempts_CreatedOn"),
                    ModifiedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Attempts_ModifiedOn"),
                    ModifiedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_Attempts_ModifiedBy"),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "klaus")
                        .Annotation("Relational:DefaultConstraintName", "DF_Attempts_CreatedBy")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attempts", x => x.AttemptId);
                    table.ForeignKey(
                        name: "FK_Attempts_Participations",
                        column: x => x.ParticipationId,
                        principalSchema: "dbo",
                        principalTable: "Participations",
                        principalColumn: "ParticipationId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Records",
                schema: "dbo",
                columns: table => new
                {
                    RecordId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EraId = table.Column<int>(type: "int", nullable: false),
                    AgeCategoryId = table.Column<int>(type: "int", nullable: false),
                    WeightCategoryId = table.Column<int>(type: "int", nullable: false),
                    RecordCategoryId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    IsStandard = table.Column<bool>(type: "bit", nullable: false),
                    AttemptId = table.Column<int>(type: "int", nullable: true),
                    IsCurrent = table.Column<bool>(type: "bit", nullable: false),
                    IsRaw = table.Column<bool>(type: "bit", nullable: false),
                    CreatedOn = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                        .Annotation("Relational:DefaultConstraintName", "DF_Records_CreatedOn"),
                    CreatedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Records", x => x.RecordId);
                    table.ForeignKey(
                        name: "FK_Records_AgeCategories",
                        column: x => x.AgeCategoryId,
                        principalSchema: "dbo",
                        principalTable: "AgeCategories",
                        principalColumn: "AgeCategoryId");
                    table.ForeignKey(
                        name: "FK_Records_Attempts",
                        column: x => x.AttemptId,
                        principalSchema: "dbo",
                        principalTable: "Attempts",
                        principalColumn: "AttemptId");
                    table.ForeignKey(
                        name: "FK_Records_Eras",
                        column: x => x.EraId,
                        principalSchema: "dbo",
                        principalTable: "Eras",
                        principalColumn: "EraId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Records_WeightCategories",
                        column: x => x.WeightCategoryId,
                        principalSchema: "dbo",
                        principalTable: "WeightCategories",
                        principalColumn: "WeightCategoryId");
                });

            migrationBuilder.CreateIndex(
                name: "_dta_index_Athletes_20_971150505__K8_K1",
                schema: "dbo",
                table: "Athletes",
                columns: new[] { "CountryId", "AthleteId" });

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_NameYearUnique",
                schema: "dbo",
                table: "Athletes",
                columns: new[] { "Firstname", "Lastname", "DateOfBirth" },
                unique: true,
                filter: "[DateOfBirth] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_Slug_Unique",
                schema: "dbo",
                table: "Athletes",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Athletes_TeamId",
                schema: "dbo",
                table: "Athletes",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "_dta_index_Attempts_20_565577053__K2_1_3_4_5_6_7_8_9_10",
                schema: "dbo",
                table: "Attempts",
                column: "ParticipationId");

            migrationBuilder.CreateIndex(
                name: "IX_EraWeightCategories_EraId",
                schema: "dbo",
                table: "EraWeightCategories",
                column: "EraId");

            migrationBuilder.CreateIndex(
                name: "IX_EraWeightCategories_WeightCategoryId",
                schema: "dbo",
                table: "EraWeightCategories",
                column: "WeightCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Meets_MeetTypeId",
                schema: "dbo",
                table: "Meets",
                column: "MeetTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Meets_Slug_Unique",
                schema: "dbo",
                table: "Meets",
                column: "Slug");

            migrationBuilder.CreateIndex(
                name: "nci_wi_Meets_096AE4AB-BAC9-480E-99C7-F2591AA73EF9",
                schema: "dbo",
                table: "Meets",
                column: "PublishedResults");

            migrationBuilder.CreateIndex(
                name: "nci_wi_Meets_B4C6AEB59052E88F812BB1BBA9EB448F",
                schema: "dbo",
                table: "Meets",
                columns: new[] { "IsRaw", "MeetTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Pages_PageGroupId",
                schema: "dbo",
                table: "Pages",
                column: "PageGroupId");

            migrationBuilder.CreateIndex(
                name: "_dta_index_Participations_20_75147313__K3_1_2_4_5_6_7_8_9_10_11_12_13_14_15_16_17_18",
                schema: "dbo",
                table: "Participations",
                column: "MeetId");

            migrationBuilder.CreateIndex(
                name: "IX_Participations_AgeCategoryId",
                schema: "dbo",
                table: "Participations",
                column: "AgeCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Participations_TeamId",
                schema: "dbo",
                table: "Participations",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_Participations_WeightCategoryId",
                schema: "dbo",
                table: "Participations",
                column: "WeightCategoryId");

            migrationBuilder.CreateIndex(
                name: "nci_wi_Participations_48291F9C-12F7-468C-BB5C-6AB9A431BCF6",
                schema: "dbo",
                table: "Participations",
                column: "ModifiedOn");

            migrationBuilder.CreateIndex(
                name: "nci_wi_Participations_6F9B90B41FACB987C7A9",
                schema: "dbo",
                table: "Participations",
                columns: new[] { "AthleteId", "Disqualified" });

            migrationBuilder.CreateIndex(
                name: "IX_Photos_MeetId",
                schema: "dbo",
                table: "Photos",
                column: "MeetId");

            migrationBuilder.CreateIndex(
                name: "IX_Records_EraId",
                schema: "dbo",
                table: "Records",
                column: "EraId");

            migrationBuilder.CreateIndex(
                name: "IX_Records_WeightCategoryId",
                schema: "dbo",
                table: "Records",
                column: "WeightCategoryId");

            migrationBuilder.CreateIndex(
                name: "nci_wi_Records_3CB8ADEAD69A6DA29B4DC80D395ABC87",
                schema: "dbo",
                table: "Records",
                columns: new[] { "IsCurrent", "EraId" });

            migrationBuilder.CreateIndex(
                name: "nci_wi_Records_40FB705E-F1AE-4E31-998B-DE4A0332DA61",
                schema: "dbo",
                table: "Records",
                columns: new[] { "AgeCategoryId", "EraId", "RecordCategoryId", "WeightCategoryId", "IsRaw", "Date" });

            migrationBuilder.CreateIndex(
                name: "nci_wi_Records_FCB2C18B-AF9A-44EE-BAE9-051E384EC259",
                schema: "dbo",
                table: "Records",
                columns: new[] { "AttemptId", "EraId", "IsCurrent" });

            migrationBuilder.CreateIndex(
                name: "IX_Teams_CountryId",
                schema: "dbo",
                table: "Teams",
                column: "CountryId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "dbo",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId",
                schema: "dbo",
                table: "UserRoles",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_UsernameUnique",
                schema: "dbo",
                table: "Users",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AthleteAliases",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Bans",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Cases",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "EraWeightCategories",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Pages",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Photos",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Records",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Wilks",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PageGroups",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Attempts",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Eras",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Participations",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "AgeCategories",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Athletes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Meets",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "WeightCategories",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Teams",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "MeetTypes",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "Countries",
                schema: "dbo");
        }
    }
}
