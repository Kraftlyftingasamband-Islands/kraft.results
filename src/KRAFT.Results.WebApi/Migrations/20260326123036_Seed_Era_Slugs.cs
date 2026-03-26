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
            migrationBuilder.Sql("UPDATE dbo.Eras SET Slug = 'pre-isi' WHERE Title = 'Pre-ISI' AND Slug IS NULL;");
            migrationBuilder.Sql("UPDATE dbo.Eras SET Slug = 'isi-old-weightcategories' WHERE Title = 'ISI Old Weightcategories' AND Slug IS NULL;");
            migrationBuilder.Sql("UPDATE dbo.Eras SET Slug = 'isi-new-weightcategories' WHERE Title = 'ISI New WeightCategories' AND Slug IS NULL;");
            migrationBuilder.Sql("UPDATE dbo.Eras SET Slug = 'pre-isi-old-weightcategories' WHERE Title = 'PRE-ISI Old WeightCategories' AND Slug IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: do not NULL out slugs on rollback to avoid data loss.
        }
    }
}
