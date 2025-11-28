using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.MeetTypes;
using KRAFT.Results.WebApi.Features.Users;
using KRAFT.Results.WebApi.Services;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.Create;

internal sealed class CreateMeetHandler
{
    private readonly ILogger<CreateMeetHandler> _logger;
    private readonly ResultsDbContext _dbContext;
    private readonly IHttpContextService _httpContextService;

    public CreateMeetHandler(ILogger<CreateMeetHandler> logger, ResultsDbContext dbContext, IHttpContextService httpContextService)
    {
        _logger = logger;
        _dbContext = dbContext;
        _httpContextService = httpContextService;
    }

    public async Task<Result<int>> Handle(CreateMeetCommand command, CancellationToken cancellationToken)
    {
        User creator = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (await IsDuplicateAsync(command.Title, command.StartDate, cancellationToken))
        {
            _logger.LogWarning("A meet with the title '{Title}' and start date '{StartDate}' already exists.", command.Title, command.StartDate);
            return MeetErrors.MeetExists(command.Title, command.StartDate);
        }

        int meetTypeId = command.MeetTypeId ?? 1;

        if (await GetMeetTypeAsync(meetTypeId, cancellationToken) is not MeetType type)
        {
            _logger.LogWarning("Meet type with Id {Id} was not found in the database", meetTypeId);
            return MeetErrors.MeetTypeNotFound;
        }

        Result<Meet> result = Meet.Create(
            creator,
            type,
            command.Title,
            command.StartDate);

        if (result.IsFailure)
        {
            return result.Error;
        }

        Meet meet = result.FromResult();

        _dbContext.Set<Meet>().Add(meet);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return meet.MeetId;
    }

    private Task<bool> IsDuplicateAsync(string title, DateOnly startDate, CancellationToken cancellationToken) =>
        _dbContext.Set<Meet>()
        .Where(x => x.Title == title)
        .Where(x => x.StartDate == startDate.ToDateTime(TimeOnly.MinValue))
        .AnyAsync(cancellationToken);

    private Task<MeetType?> GetMeetTypeAsync(int meetTypeId, CancellationToken cancellationToken) =>
        _dbContext.Set<MeetType>()
        .Where(x => x.MeetTypeId == meetTypeId)
        .FirstOrDefaultAsync(cancellationToken);
}