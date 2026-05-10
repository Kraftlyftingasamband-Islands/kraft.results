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
        Result<User> modifierResult = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (modifierResult.IsFailure)
        {
            return Result.Failure(modifierResult.Error);
        }

        User modifier = modifierResult.FromResult();

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

        MeetCategory category = (MeetCategory)(command.MeetTypeId ?? 1);

        if (!Enum.IsDefined(category))
        {
            _logger.LogWarning("Meet category {Id} is not a valid MeetCategory value", command.MeetTypeId);
            return Result.Failure(MeetErrors.MeetTypeNotFound);
        }

        if (_dbContext.Entry(meet).Property("MeetId").CurrentValue is not int meetId)
        {
            _logger.LogError("MeetId shadow property missing or wrong type for slug {Slug}", slug);
            return Result.Failure(MeetErrors.MeetNotFound);
        }

        Result result = meet.Update(
            meetId,
            modifier,
            category,
            command.Title,
            command.StartDate,
            command.EndDate,
            command.CalcPlaces,
            command.Text,
            command.Location,
            command.PublishedResults,
            command.ResultModeId,
            command.PublishedInCalendar,
            command.IsInTeamCompetition,
            command.ShowWilks,
            command.ShowTeamPoints,
            command.ShowBodyWeight,
            command.ShowTeams,
            command.RecordsPossible,
            command.IsRaw);

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
}