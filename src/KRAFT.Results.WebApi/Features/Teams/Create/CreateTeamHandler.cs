using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Teams.Create;

internal sealed class CreateTeamHandler
{
    private readonly ILogger<CreateTeamHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public CreateTeamHandler(ILogger<CreateTeamHandler> logger, ResultsDbContext dbContext, IHttpContextService httpContextService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
    }

    public async Task<Result<int>> Handle(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        Result<User> creatorResult = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (creatorResult.IsFailure)
        {
            return creatorResult.Error;
        }

        User creator = creatorResult.FromResult();

        if (await _dbContext.GetCountryAsync(command.CountryId, cancellationToken) is not Country country)
        {
            _logger.LogWarning(
                "Failed to create team {Title}: Country with Id {CountryId} does not exist",
                command.Title,
                command.CountryId);

            return CountryErrors.CountryDoesNotExist(command.CountryId);
        }

        if (await _dbContext.Set<Team>().AnyAsync(x => x.TitleShort == command.TitleShort, cancellationToken: cancellationToken))
        {
            _logger.LogWarning("Short title {Title} already exists", command.TitleShort);
            return TeamErrors.ShortTitleExists;
        }

        Result<Team> result = Team.Create(
            creator: creator,
            title: command.Title,
            titleShort: command.TitleShort,
            titleFull: command.TitleFull,
            country: country);

        if (result.IsFailure)
        {
            return result.Error;
        }

        Team team = result.FromResult();

        _dbContext.Set<Team>().Add(team);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return team.TeamId;
    }
}