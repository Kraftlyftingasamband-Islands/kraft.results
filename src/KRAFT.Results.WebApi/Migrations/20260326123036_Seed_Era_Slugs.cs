using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class Seed_Era_Slugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE dbo.Eras SET Slug = 'fyrir-isi', Title = 'Fyrir ÍSÍ' WHERE Title = 'Pre-ISI';");
            migrationBuilder.Sql("UPDATE dbo.Eras SET Slug = 'isi-gamalt', Title = 'ÍSÍ (gamalt)' WHERE Title = 'ISI Old Weightcategories';");
            migrationBuilder.Sql("UPDATE dbo.Eras SET Slug = 'isi-nuverandi', Title = 'ÍSÍ (núverandi)' WHERE Title = 'ISI New WeightCategories';");
            migrationBuilder.Sql("UPDATE dbo.Eras SET Slug = 'fyrir-isi-gamalt', Title = 'Fyrir ÍSÍ (gamalt)' WHERE Title = 'PRE-ISI Old WeightCategories';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: do not NULL out slugs on rollback to avoid data loss.
        }
    }
}
