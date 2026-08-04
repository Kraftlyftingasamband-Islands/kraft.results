using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Records;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Athletes.GetRecords;

internal sealed class GetAthleteRecordsHandler(ResultsDbContext dbContext)
{
    public async Task<List<AthleteRecord>> Handle(string slug, CancellationToken cancellationToken)
    {
        var rows = await dbContext.Set<Record>()
            .AsNoTracking()
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
                IsSingleLift = x.Attempt.Participation.Meet.Category != MeetCategory.Powerlifting,
                WeightCategory = x.WeightCategory.Title,
                AgeCategorySlug = x.AgeCategory.Slug,
                AgeCategoryTitle = x.AgeCategory.Title,
                Gender = x.Attempt.Participation.Athlete.Gender,
                x.Weight,
                x.Attempt.Discipline,
                x.RecordCategoryId,
                MeetTitle = x.Attempt.Participation.Meet.Title,
                MeetYear = x.Attempt.Participation.Meet.StartDate.Year,
                MeetSlug = x.Attempt.Participation.Meet.Slug,
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(x =>
            {
                string ageCategory = DisplayNames.ResolveAgeCategoryLabel(x.AgeCategorySlug, x.AgeCategoryTitle, x.Gender);

                return new AthleteRecord(
                    x.Date,
                    x.IsClassic,
                    x.IsSingleLift,
                    IsWithinPowerlifting: (x.RecordCategoryId == RecordCategory.BenchSingle || x.RecordCategoryId == RecordCategory.DeadliftSingle) && !x.IsSingleLift,
                    IsStandaloneDiscipline: x.RecordCategoryId == RecordCategory.Bench || x.RecordCategoryId == RecordCategory.Deadlift,
                    x.WeightCategory,
                    ageCategory,
                    MapRecordType(x.RecordCategoryId),
                    x.Weight,
                    $"{x.MeetTitle} {x.MeetYear}",
                    x.MeetSlug);
            })
            .ToList();
    }

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
