using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Records;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Athletes.GetRecords;

internal sealed class GetAthleteRecordsHandler(ResultsDbContext dbContext)
{
    public Task<List<AthleteRecord>> Handle(string slug, CancellationToken cancellationToken) =>
        dbContext.Set<Record>()
        .Where(x => x.Attempt!.Participation.Athlete.Slug == slug)
        .Where(x => x.IsCurrent)
        .Where(x => x.Era.EndDate.Year > DateTime.UtcNow.Year)
        .OrderByDescending(x => x.Attempt!.Participation.Meet.StartDate)
        .ThenBy(x => x.WeightCategoryId)
        .ThenBy(x => x.AgeCategory.AgeCategoryId)
        .ThenBy(x => x.Attempt!.Discipline)
        .Select(x => new
        {
            x.Date,
            IsClassic = x.Attempt!.Participation.Meet.IsRaw,
            IsSingleLift = IsSingleLift(x.Attempt!.Participation.Meet.MeetType.Title),
            WeightCategory = x.WeightCategory.Title,
            AgeCategory = x.AgeCategory.Title,
            x.Attempt!.Participation.Total,
            x.Weight,
            x.Attempt!.Discipline,
            x.RecordCategoryId,
            MeetTitle = x.Attempt!.Participation.Meet.Title,
            MeetYear = x.Attempt!.Participation.Meet.StartDate.Year,
            MeetSlug = x.Attempt!.Participation.Meet.Slug,
        })
        .Select(x => new AthleteRecord(
            x.Date,
            x.IsClassic,
            x.IsSingleLift,
            IsWithinPowerlifting: (x.RecordCategoryId == RecordCategory.BenchSingle || x.RecordCategoryId == RecordCategory.DeadliftSingle) && !x.IsSingleLift,
            IsStandaloneDiscipline: x.RecordCategoryId == RecordCategory.Bench || x.RecordCategoryId == RecordCategory.Deadlift,
            x.WeightCategory,
            x.AgeCategory,
            MapRecordType(x.RecordCategoryId),
            x.Weight,
            $"{x.MeetTitle} {x.MeetYear}",
            x.MeetSlug))
        .ToListAsync(cancellationToken);

    private static bool IsSingleLift(string meetType) => meetType != "Powerlifting";

#pragma warning disable S3358 // Ternary operators should not be nested
    private static string MapRecordType(RecordCategory category) =>
        category == RecordCategory.Squat ? Constants.Squat
        : category == RecordCategory.Bench ? Constants.Bench
        : category == RecordCategory.Deadlift ? Constants.Deadlift
        : category == RecordCategory.Total ? Constants.Total
        : category == RecordCategory.BenchSingle ? Constants.Bench
        : category == RecordCategory.DeadliftSingle ? Constants.Deadlift
        : string.Empty;
#pragma warning restore S3358 // Ternary operators should not be nested
}