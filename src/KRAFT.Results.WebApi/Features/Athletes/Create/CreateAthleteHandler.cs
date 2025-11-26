using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Teams;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Athletes.Create;

internal sealed class CreateAthleteHandler
{
    private readonly ILogger<CreateAthleteHandler> _logger;
    private readonly ResultsDbContext _dbContext;

    public CreateAthleteHandler(ILogger<CreateAthleteHandler> logger, ResultsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Result<int>> Handle(CreateAthleteCommand command)
    {
        if (await _dbContext.Set<Country>().FirstOrDefaultAsync(x => x.CountryId == command.CountryId) is not Country country)
        {
            _logger.LogWarning(
                "Failed to create athlete {First} {Last}: Country with Id {CountryId} does not exist",
                command.FirstName,
                command.LastName,
                command.CountryId);

            return CountryErrors.CountryDoesNotExist(command.CountryId);
        }

        Team? team = command.TeamId.HasValue
            ? await _dbContext.Set<Team>().FirstOrDefaultAsync(x => x.TeamId == command.TeamId.Value)
            : null;

        if (command.TeamId.HasValue && team is null)
        {
            _logger.LogWarning(
                "Failed to create athlete {First} {Last}: Team with Id {TeamId} does not exist",
                command.FirstName,
                command.LastName,
                command.TeamId);

            return TeamErrors.TeamDoesNotExist(command.TeamId.Value);
        }

        Result<Athlete> result = Athlete.Create(
            firstName: command.FirstName,
            lastName: command.LastName,
            gender: command.Gender,
            country: country,
            team: team,
            dateOfBirth: command.DateOfBirth);

        if (result.IsFailure)
        {
            return result.Error;
        }

        Athlete athlete = result.FromResult();
        _dbContext.Set<Athlete>().Add(athlete);

        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Created athlete {First} {Last} with Id {AthleteId}", athlete.Firstname, athlete.Lastname, athlete.AthleteId);

        return athlete.AthleteId;
    }
}