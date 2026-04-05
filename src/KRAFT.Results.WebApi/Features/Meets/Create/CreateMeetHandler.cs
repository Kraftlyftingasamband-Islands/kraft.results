using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Abstractions;
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

    public async Task<Result<string>> Handle(CreateMeetCommand command, CancellationToken cancellationToken)
    {
        Result<User> creatorResult = await _dbContext.GetUserAsync(_httpContextService, cancellationToken);

        if (creatorResult.IsFailure)
        {
            return creatorResult.Error;
        }

        User creator = creatorResult.FromResult();

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
            return result.Error;
        }

        Meet meet = result.FromResult();

        _dbContext.Set<Meet>().Add(meet);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return meet.Slug;
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