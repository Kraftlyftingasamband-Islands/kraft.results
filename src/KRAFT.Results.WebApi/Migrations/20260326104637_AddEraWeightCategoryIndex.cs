using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KRAFT.Results.WebApi.Migrations
{
    /// <inheritdoc />
    public partial class AddEraWeightCategoryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EraWeightCategories_EraId' AND object_id = OBJECT_ID('dbo.EraWeightCategories'))
                    DROP INDEX [IX_EraWeightCategories_EraId] ON [dbo].[EraWeightCategories];

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EraWeightCategories_EraId_WeightCategoryId' AND object_id = OBJECT_ID('dbo.EraWeightCategories'))
                    CREATE INDEX [IX_EraWeightCategories_EraId_WeightCategoryId] ON [dbo].[EraWeightCategories] ([EraId], [WeightCategoryId]);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EraWeightCategories_EraId_WeightCategoryId' AND object_id = OBJECT_ID('dbo.EraWeightCategories'))
                    DROP INDEX [IX_EraWeightCategories_EraId_WeightCategoryId] ON [dbo].[EraWeightCategories];

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_EraWeightCategories_EraId' AND object_id = OBJECT_ID('dbo.EraWeightCategories'))
                    CREATE INDEX [IX_EraWeightCategories_EraId] ON [dbo].[EraWeightCategories] ([EraId]);
                """);
        }
    }
}
