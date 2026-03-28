using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Teams;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Athletes.Update;

internal sealed class UpdateAthleteHandler
{
    private readonly ILogger<UpdateAthleteHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public UpdateAthleteHandler(ILogger<UpdateAthleteHandler> logger, ResultsDbContext dbContext, IHttpContextService httpContextService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
    }

    public async Task<Result> Handle(string slug, UpdateAthleteCommand command, CancellationToken cancellationToken)
    {
        User modifier = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        Athlete? athlete = await _dbContext.Set<Athlete>()
            .Where(x => x.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);

        if (athlete is null)
        {
            _logger.LogWarning("Athlete with slug '{Slug}' was not found", slug);
            return Result.Failure(AthleteErrors.AthleteNotFound);
        }

        if (await _dbContext.GetCountryAsync(command.CountryId, cancellationToken) is not Country country)
        {
            _logger.LogWarning(
                "Failed to update athlete {Slug}: Country with Id {CountryId} does not exist",
                slug,
                command.CountryId);

            return CountryErrors.CountryDoesNotExist(command.CountryId);
        }

        Team? team = command.TeamId is int teamId
            ? await GetTeamAsync(teamId, cancellationToken)
            : null;

        if (command.TeamId.HasValue && team is null)
        {
            _logger.LogWarning(
                "Failed to update athlete {Slug}: Team with Id {TeamId} does not exist",
                slug,
                command.TeamId);

            return Result.Failure(TeamErrors.TeamDoesNotExist(command.TeamId.Value));
        }

        Result result = athlete.Update(
            modifier: modifier,
            firstName: command.FirstName,
            lastName: command.LastName,
            gender: command.Gender,
            country: country,
            dateOfBirth: command.DateOfBirth,
            team: team);

        if (result.IsFailure)
        {
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private Task<Team?> GetTeamAsync(int teamId, CancellationToken cancellationToken) =>
        _dbContext.Set<Team>()
        .Where(x => x.TeamId == teamId)
        .FirstOrDefaultAsync(cancellationToken);
}