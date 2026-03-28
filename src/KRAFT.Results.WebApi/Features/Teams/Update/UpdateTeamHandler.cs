using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Countries;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Teams.Update;

internal sealed class UpdateTeamHandler
{
    private readonly ILogger<UpdateTeamHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public UpdateTeamHandler(ILogger<UpdateTeamHandler> logger, ResultsDbContext dbContext, IHttpContextService httpContextService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
    }

    public async Task<Result> Handle(string slug, UpdateTeamCommand command, CancellationToken cancellationToken)
    {
        Result<User> modifierResult = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (modifierResult.IsFailure)
        {
            return Result.Failure(modifierResult.Error);
        }

        User modifier = modifierResult.FromResult();

        Team? team = await _dbContext.Set<Team>()
            .Where(x => x.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);

        if (team is null)
        {
            _logger.LogWarning("Team with slug '{Slug}' was not found", slug);
            return Result.Failure(TeamErrors.TeamNotFound);
        }

        if (await _dbContext.GetCountryAsync(command.CountryId, cancellationToken) is not Country country)
        {
            _logger.LogWarning(
                "Failed to update team {Slug}: Country with Id {CountryId} does not exist",
                slug,
                command.CountryId);

            return CountryErrors.CountryDoesNotExist(command.CountryId);
        }

        if (await IsDuplicateShortTitleAsync(slug, command.TitleShort, cancellationToken))
        {
            _logger.LogWarning("Short title {TitleShort} already exists", command.TitleShort);
            return Result.Failure(TeamErrors.ShortTitleExists);
        }

        Result result = team.Update(modifier, command.Title, command.TitleShort, command.TitleFull, country);

        if (result.IsFailure)
        {
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private Task<bool> IsDuplicateShortTitleAsync(string currentSlug, string titleShort, CancellationToken cancellationToken) =>
        _dbContext.Set<Team>()
        .Where(x => x.Slug != currentSlug)
        .Where(x => x.TitleShort == titleShort)
        .AnyAsync(cancellationToken);
}