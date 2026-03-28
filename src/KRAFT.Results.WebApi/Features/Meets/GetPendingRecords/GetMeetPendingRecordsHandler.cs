using KRAFT.Results.Contracts.Records;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Records;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.GetPendingRecords;

internal sealed class GetMeetPendingRecordsHandler(ResultsDbContext dbContext)
{
    public async Task<List<PendingRecordEntry>?> Handle(string slug, CancellationToken cancellationToken)
    {
        bool meetExists = await dbContext.Set<Meet>()
            .AnyAsync(m => m.Slug == slug, cancellationToken);

        if (!meetExists)
        {
            return null;
        }

        List<PendingRecordEntry> entries = await dbContext.Set<Record>()
            .Where(r => r.Status == RecordStatus.Pending)
            .Where(r => r.Attempt != null)
            .Where(r => r.Attempt!.Participation.Meet.Slug == slug)
            .Select(r => new PendingRecordEntry(
                r.RecordId,
                r.Attempt!.Participation.Athlete.Firstname + " " + r.Attempt.Participation.Athlete.Lastname,
                r.RecordCategoryId.ToDisplayName(),
                r.Weight,
                r.WeightCategory.Title,
                r.AgeCategory.Title))
            .ToListAsync(cancellationToken);

        return entries;
    }
}