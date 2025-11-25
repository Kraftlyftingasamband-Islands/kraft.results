using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.Core;

public sealed class ResultsDbContext(DbContextOptions<ResultsDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResultsDbContext).Assembly);
    }
}