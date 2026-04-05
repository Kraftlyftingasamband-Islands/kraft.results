using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.AgeCategories;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.UpdateAgeCategory;

internal sealed class UpdateAgeCategoryHandler
{
    private readonly ILogger<UpdateAgeCategoryHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public UpdateAgeCategoryHandler(
        ILogger<UpdateAgeCategoryHandler> logger,
        ResultsDbContext dbContext,
        IHttpContextService httpContextService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
    }

    public async Task<Result> Handle(
        int meetId,
        int participationId,
        UpdateAgeCategoryCommand command,
        CancellationToken cancellationToken)
    {
        AgeCategory? ageCategory = await _dbContext.Set<AgeCategory>()
            .FirstOrDefaultAsync(ac => ac.Slug == command.Slug, cancellationToken);

        if (ageCategory is null)
        {
            return MeetErrors.AgeCategoryNotFound;
        }

        Participation? participation = await _dbContext.Set<Participation>()
            .FirstOrDefaultAsync(
                p => p.ParticipationId == participationId && p.MeetId == meetId,
                cancellationToken);

        if (participation is null)
        {
            _logger.LogWarning(
                "Participation {ParticipationId} not found for meet {MeetId}",
                participationId,
                meetId);
            return MeetErrors.ParticipationNotFound;
        }

        Result<User> userResult = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (userResult.IsFailure)
        {
            return Result.Failure(userResult.Error);
        }

        User user = userResult.FromResult();

        participation.UpdateAgeCategory(ageCategory.AgeCategoryId, user.Username);

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated age category for participation {ParticipationId} in meet {MeetId} to {Slug}",
            participationId,
            meetId,
            command.Slug);

        return Result.Success();
    }
}