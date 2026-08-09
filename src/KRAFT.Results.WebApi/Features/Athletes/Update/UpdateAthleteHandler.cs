using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Clubs;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;
using KRAFT.Results.WebApi.ValueObjects;

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
        Result<User> modifierResult = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (modifierResult.IsFailure)
        {
            return Result.Failure(modifierResult.Error);
        }

        User modifier = modifierResult.FromResult();

        Athlete? athlete = await _dbContext.Set<Athlete>()
            .Where(x => x.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);

        if (athlete is null)
        {
            _logger.LogWarning("Athlete with slug '{Slug}' was not found", slug);
            return Result.Failure(AthleteErrors.AthleteNotFound);
        }

        Result<Country> countryResult = Country.FromCode(command.CountryCode);

        if (countryResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to update athlete {Slug}: Country code '{CountryCode}' is invalid",
                slug,
                command.CountryCode);

            return Result.Failure(countryResult.Error);
        }

        Country country = countryResult.FromResult();

        Club? club = command.ClubId is int clubId
            ? await GetClubAsync(clubId, cancellationToken)
            : null;

        if (command.ClubId.HasValue && club is null)
        {
            _logger.LogWarning(
                "Failed to update athlete {Slug}: Club with Id {ClubId} does not exist",
                slug,
                command.ClubId);

            return Result.Failure(ClubErrors.ClubDoesNotExist(command.ClubId.Value));
        }

        Result result = athlete.Update(
            modifier: modifier,
            firstName: command.FirstName,
            lastName: command.LastName,
            gender: command.Gender,
            country: country,
            dateOfBirth: command.DateOfBirth,
            club: club);

        if (result.IsFailure)
        {
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private Task<Club?> GetClubAsync(int clubId, CancellationToken cancellationToken) =>
        _dbContext.Set<Club>()
        .Where(x => x.ClubId == clubId)
        .FirstOrDefaultAsync(cancellationToken);
}
