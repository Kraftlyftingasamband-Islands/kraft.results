using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.Core;

#pragma warning disable CA1515 // Consider making public types internal
public sealed class ResultsDbContext(DbContextOptions<ResultsDbContext> options) : DbContext(options)
#pragma warning restore CA1515 // Consider making public types internal
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResultsDbContext).Assembly);
    }
}