using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Countries;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Teams.Create;

internal sealed class CreateTeamHandler
{
    private readonly ILogger<CreateTeamHandler> _logger;
    private readonly ResultsDbContext _dbContext;

    public CreateTeamHandler(ILogger<CreateTeamHandler> logger, ResultsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Result<int>> Handle(CreateTeamCommand command, CancellationToken cancellationToken)
    {
        if (await _dbContext.Set<Country>().FirstOrDefaultAsync(x => x.CountryId == command.CountryId, cancellationToken) is not Country country)
        {
            _logger.LogWarning(
                "Failed to create team {Title}: Country with Id {CountryId} does not exist",
                command.Title,
                command.CountryId);

            return CountryErrors.CountryDoesNotExist(command.CountryId);
        }

        if (await _dbContext.Set<Team>().AnyAsync(x => x.TitleShort == command.TitleShort, cancellationToken: cancellationToken))
        {
            return TeamErrors.ShortTitleExists(command.TitleShort);
        }

        Result<Team> result = Team.Create(
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