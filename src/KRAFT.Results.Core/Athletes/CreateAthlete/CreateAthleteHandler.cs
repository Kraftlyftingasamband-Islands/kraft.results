using KRAFT.Results.Core.Athletes.AddAthlete;

using Microsoft.Extensions.Logging;

namespace KRAFT.Results.Core.Athletes.CreateAthlete;

internal sealed class CreateAthleteHandler
{
    private readonly ILogger<CreateAthleteHandler> logger;
    private readonly ResultsDbContext dbContext;

    public CreateAthleteHandler(ILogger<CreateAthleteHandler> logger, ResultsDbContext dbContext)
    {
        this.logger = logger;
        this.dbContext = dbContext;
    }

    public async Task Handle(CreateAthleteCommand command, CancellationToken cancellationToken)
    {
        Athlete athlete = new()
        {
            FirstName = command.FirstName,
            LastName = command.LastName,
        };

        this.dbContext.Athletes.Add(athlete);

        await this.dbContext.SaveChangesAsync(cancellationToken);
    }
}