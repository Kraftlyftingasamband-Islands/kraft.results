using KRAFT.Results.Contracts.Clubs;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Clubs.Update;

internal sealed class UpdateClubHandler
{
    private readonly ILogger<UpdateClubHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public UpdateClubHandler(ILogger<UpdateClubHandler> logger, ResultsDbContext dbContext, IHttpContextService httpContextService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
    }

    public async Task<Result> Handle(string slug, UpdateClubCommand command, CancellationToken cancellationToken)
    {
        Result<User> modifierResult = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (modifierResult.IsFailure)
        {
            return Result.Failure(modifierResult.Error);
        }

        User modifier = modifierResult.FromResult();

        Club? club = await _dbContext.Set<Club>()
            .Where(x => x.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);

        if (club is null)
        {
            _logger.LogWarning("Club with slug '{Slug}' was not found", slug);
            return Result.Failure(ClubErrors.ClubNotFound);
        }

        Result<Country> countryResult = Country.FromCode(command.CountryCode);

        if (countryResult.IsFailure)
        {
            _logger.LogWarning(
                "Failed to update club {Slug}: Country code '{CountryCode}' is invalid",
                slug,
                command.CountryCode);

            return Result.Failure(countryResult.Error);
        }

        Country country = countryResult.FromResult();

        if (await IsDuplicateShortTitleAsync(slug, command.TitleShort, cancellationToken))
        {
            _logger.LogWarning("Short title {TitleShort} already exists", command.TitleShort);
            return Result.Failure(ClubErrors.ShortTitleExists);
        }

        if (await IsDuplicateTitleAsync(slug, command.Title, cancellationToken))
        {
            _logger.LogWarning("Title {Title} already exists", command.Title);
            return Result.Failure(ClubErrors.TitleExists);
        }

        Result result = club.Update(modifier, command.Title, command.TitleShort, command.TitleFull, country);

        if (result.IsFailure)
        {
            return result;
        }

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

            return Result.Failure(error);
        }

        return Result.Success();
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

    private Task<bool> IsDuplicateShortTitleAsync(string currentSlug, string titleShort, CancellationToken cancellationToken) =>
        _dbContext.Set<Club>()
        .Where(x => x.Slug != currentSlug)
        .Where(x => x.TitleShort == titleShort)
        .AnyAsync(cancellationToken);

    private Task<bool> IsDuplicateTitleAsync(string currentSlug, string title, CancellationToken cancellationToken) =>
        _dbContext.Set<Club>()
        .Where(x => x.Slug != currentSlug)
        .Where(x => x.Title == title)
        .AnyAsync(cancellationToken);
}
