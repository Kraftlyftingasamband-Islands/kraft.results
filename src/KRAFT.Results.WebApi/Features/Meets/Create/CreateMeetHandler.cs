using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.MeetTypes;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.Create;

internal sealed class CreateMeetHandler
{
    private readonly ILogger<CreateMeetHandler> _logger;
    private readonly ResultsDbContext _dbContext;

    public CreateMeetHandler(ILogger<CreateMeetHandler> logger, ResultsDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<Result<int>> Handle(CreateMeetCommand command, CancellationToken cancellationToken)
    {
        if (await _dbContext.Set<Meet>().AnyAsync(x => x.Title == command.Title && x.StartDate == command.StartDate.ToDateTime(TimeOnly.MinValue), cancellationToken))
        {
            _logger.LogWarning("A meet with the title '{Title}' and start date '{StartDate}' already exists.", command.Title, command.StartDate);
            return MeetErrors.MeetExists(command.Title, command.StartDate);
        }

        int meetTypeId = command.MeetTypeId ?? 1;

        if (await _dbContext.Set<MeetType>().FirstOrDefaultAsync(x => x.MeetTypeId == meetTypeId, cancellationToken) is not MeetType type)
        {
            _logger.LogWarning("Meet type with Id {Id} was not found in the database", meetTypeId);
            return MeetErrors.MeetTypeNotFound;
        }

        Result<Meet> result = Meet.Create(
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
}