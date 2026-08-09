using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Clubs.Create;

internal sealed class CreateClubHandler
{
    private readonly ILogger<CreateClubHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public CreateClubHandler(ILogger<CreateClubHandler> logger, ResultsDbContext dbContext, IHttpContextService httpContextService)
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

        Result<Country> countryResult = Country.FromCode(command.CountryCode);

        if (countryResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to create club {Title}: Country code '{CountryCode}' is invalid",
                command.Title,
                command.CountryCode);

            return countryResult.Error;
        }

        Country country = countryResult.FromResult();

        if (await _dbContext.Set<Club>().AnyAsync(x => x.TitleShort == command.TitleShort, cancellationToken: cancellationToken))
        {
            _logger.LogWarning("Short title {Title} already exists", command.TitleShort);
            return ClubErrors.ShortTitleExists;
        }

        if (await _dbContext.Set<Club>().AnyAsync(x => x.Title == command.Title, cancellationToken: cancellationToken))
        {
            _logger.LogWarning("Title {Title} already exists", command.Title);
            return ClubErrors.TitleExists;
        }

        Result<Club> result = Club.Create(
            creator: creator,
            title: command.Title,
            titleShort: command.TitleShort,
            titleFull: command.TitleFull,
            country: country);

        if (result.IsFailure)
        {
            return result.Error;
        }

        Club club = result.FromResult();

        _dbContext.Set<Club>().Add(club);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 } sqlEx)
        {
            Error? error = UniqueConstraintToError(sqlEx.Message);

            if (error is null)
            {
                _logger.LogWarning(ex, "Unexpected unique constraint violation on Teams");
                throw;
            }

            return error;
        }

        return club.ClubId;
    }

    private static Error? UniqueConstraintToError(string message)
    {
        if (message.Contains("IX_Teams_TitleShort_Unique", StringComparison.Ordinal))
        {
            return ClubErrors.ShortTitleExists;
        }

        if (message.Contains("IX_Teams_Title_Unique", StringComparison.Ordinal) || message.Contains("IX_Teams_Slug_Unique", StringComparison.Ordinal))
        {
            return ClubErrors.TitleExists;
        }

        return null;
    }
}
