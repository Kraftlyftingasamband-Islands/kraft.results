using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.Update;

internal sealed class UpdateMeetHandler
{
    private readonly ILogger<UpdateMeetHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public UpdateMeetHandler(ILogger<UpdateMeetHandler> logger, ResultsDbContext dbContext, IHttpContextService httpContextService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
    }

    public async Task<Result> Handle(string slug, UpdateMeetCommand command, CancellationToken cancellationToken)
    {
        User modifier = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        Meet? meet = await _dbContext.Set<Meet>()
            .Where(x => x.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);

        if (meet is null)
        {
            _logger.LogWarning("Meet with slug '{Slug}' was not found", slug);
            return Result.Failure(MeetErrors.MeetNotFound);
        }

        if (await IsDuplicateAsync(slug, command.Title, command.StartDate, cancellationToken))
        {
            _logger.LogWarning("A meet with the title '{Title}' and start date '{StartDate}' already exists.", command.Title, command.StartDate);
            return Result.Failure(MeetErrors.MeetExists(command.Title, command.StartDate));
        }

        int meetTypeId = command.MeetTypeId ?? 1;

        if (await GetMeetTypeAsync(meetTypeId, cancellationToken) is not MeetType type)
        {
            _logger.LogWarning("Meet type with Id {Id} was not found in the database", meetTypeId);
            return Result.Failure(MeetErrors.MeetTypeNotFound);
        }

        Result result = meet.Update(modifier, type, command.Title, command.StartDate);

        if (result.IsFailure)
        {
            return result;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private Task<bool> IsDuplicateAsync(string currentSlug, string title, DateOnly startDate, CancellationToken cancellationToken) =>
        _dbContext.Set<Meet>()
        .Where(x => x.Slug != currentSlug)
        .Where(x => x.Title == title)
        .Where(x => x.StartDate == startDate.ToDateTime(TimeOnly.MinValue))
        .AnyAsync(cancellationToken);

    private Task<MeetType?> GetMeetTypeAsync(int meetTypeId, CancellationToken cancellationToken) =>
        _dbContext.Set<MeetType>()
        .Where(x => x.MeetTypeId == meetTypeId)
        .FirstOrDefaultAsync(cancellationToken);
}