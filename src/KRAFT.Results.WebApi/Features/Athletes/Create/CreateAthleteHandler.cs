using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Teams;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Athletes.Create;

internal sealed class CreateAthleteHandler
{
    private readonly ILogger<CreateAthleteHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public CreateAthleteHandler(ILogger<CreateAthleteHandler> logger, ResultsDbContext dbContext, IHttpContextService httpContextService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
    }

    public async Task<Result<int>> Handle(CreateAthleteCommand command, CancellationToken cancellationToken)
    {
        Result<User> creatorResult = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (creatorResult.IsFailure)
        {
            return creatorResult.Error;
        }

        User creator = creatorResult.FromResult();

        if (await IsDuplicateAthlete(command.FirstName, command.LastName, command.DateOfBirth, cancellationToken))
        {
            _logger.LogWarning(
                "Failed to create athlete {First} {Last}: Athlete with date of birth {DateOfBirth:yyyy-MM-dd} already exists",
                command.FirstName,
                command.LastName,
                command.DateOfBirth);
            return AthleteErrors.AlreadyExists;
        }

        if (await _dbContext.GetCountryAsync(command.CountryId, cancellationToken) is not Country country)
        {
            _logger.LogWarning(
                "Failed to create athlete {First} {Last}: Country with Id {CountryId} does not exist",
                command.FirstName,
                command.LastName,
                command.CountryId);

            return CountryErrors.CountryDoesNotExist(command.CountryId);
        }

        Team? team = command.TeamId is int teamId
            ? await GetTeamAsync(teamId, cancellationToken)
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
            creator: creator,
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

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created athlete {First} {Last} with Id {AthleteId}", athlete.Firstname, athlete.Lastname, athlete.AthleteId);

        return athlete.AthleteId;
    }

    private Task<Team?> GetTeamAsync(int teamId, CancellationToken cancellationToken) =>
        _dbContext.Set<Team>()
        .Where(x => x.TeamId == teamId)
        .FirstOrDefaultAsync(cancellationToken);

    private Task<bool> IsDuplicateAthlete(string firstName, string lastName, DateOnly dateOfBirth, CancellationToken cancellationToken) =>
        _dbContext.Set<Athlete>()
        .Where(x => x.Firstname == firstName)
        .Where(x => x.Lastname == lastName)
        .Where(x => x.DateOfBirth == null || x.DateOfBirth == dateOfBirth)
        .AnyAsync(cancellationToken);
}