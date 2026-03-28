using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Eras;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Participations;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Records.DetectRecords;

internal sealed class RecordDetectionService(
    ResultsDbContext dbContext,
    ILogger<RecordDetectionService> logger)
{
    public async Task DetectAsync(
        Participation participation,
        Attempt attempt,
        Meet meet,
        string createdBy,
        CancellationToken cancellationToken)
    {
        if (!meet.RecordsPossible)
        {
            return;
        }

        if (!attempt.Good)
        {
            return;
        }

        RecordCategory recordCategory = MapDisciplineToRecordCategory(attempt.Discipline);

        if (recordCategory == RecordCategory.None)
        {
            return;
        }

        DateOnly meetDate = DateOnly.FromDateTime(meet.StartDate);

        Era? era = await dbContext.Set<Era>()
            .Where(e => e.StartDate <= meetDate)
            .Where(e => e.EndDate >= meetDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (era is null)
        {
            logger.LogWarning(
                "No era found for meet date {MeetDate}, skipping record detection",
                meetDate);
            return;
        }

        decimal? currentApprovedWeight = await dbContext.Set<Record>()
            .Where(r => r.Status == RecordStatus.Approved)
            .Where(r => r.EraId == era.EraId)
            .Where(r => r.AgeCategoryId == participation.AgeCategoryId)
            .Where(r => r.WeightCategoryId == participation.WeightCategoryId)
            .Where(r => r.RecordCategoryId == recordCategory)
            .Where(r => r.IsRaw == meet.IsRaw)
            .MaxAsync(r => (decimal?)r.Weight, cancellationToken);

        if (currentApprovedWeight.HasValue && attempt.Weight <= currentApprovedWeight.Value)
        {
            return;
        }

        Record? existingPending = await dbContext.Set<Record>()
            .Where(r => r.Status == RecordStatus.Pending)
            .Where(r => r.EraId == era.EraId)
            .Where(r => r.AgeCategoryId == participation.AgeCategoryId)
            .Where(r => r.WeightCategoryId == participation.WeightCategoryId)
            .Where(r => r.RecordCategoryId == recordCategory)
            .Where(r => r.IsRaw == meet.IsRaw)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingPending is not null)
        {
            existingPending.UpdatePending(attempt.AttemptId, attempt.Weight, meetDate);

            logger.LogInformation(
                "Updated existing pending record {RecordId} with new weight {Weight}",
                existingPending.RecordId,
                attempt.Weight);
        }
        else
        {
            Record pending = Record.CreatePending(
                era.EraId,
                participation.AgeCategoryId,
                participation.WeightCategoryId,
                recordCategory,
                attempt.Weight,
                meetDate,
                attempt.AttemptId,
                meet.IsRaw,
                createdBy);

            dbContext.Set<Record>().Add(pending);

            logger.LogInformation(
                "Created pending record for {Discipline} with weight {Weight}",
                attempt.Discipline,
                attempt.Weight);
        }
    }

    private static RecordCategory MapDisciplineToRecordCategory(Discipline discipline) => discipline switch
    {
        Discipline.Squat => RecordCategory.Squat,
        Discipline.Bench => RecordCategory.Bench,
        Discipline.Deadlift => RecordCategory.Deadlift,
        _ => RecordCategory.None,
    };
}