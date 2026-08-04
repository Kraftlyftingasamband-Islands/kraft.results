using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Records;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Meets.GetRecords;

internal sealed class GetMeetRecordsHandler(ResultsDbContext dbContext)
{
    private static readonly RecordCategory[] ExcludedCategories =
    [
        RecordCategory.TotalWilks,
        RecordCategory.TotalIpfPoints,
    ];

    public async Task<List<MeetRecordEntry>?> Handle(string slug, CancellationToken cancellationToken)
    {
        bool meetExists = await dbContext.Set<Meet>()
            .AnyAsync(m => m.Slug == slug, cancellationToken);

        if (!meetExists)
        {
            return null;
        }

        var rows = await dbContext.Set<Record>()
            .AsNoTracking()
            .Where(r => r.Attempt != null)
            .Where(r => r.Attempt!.Participation.Meet.Slug == slug)
            .Where(r => !r.IsStandard)
            .Where(r => !ExcludedCategories.Contains(r.RecordCategoryId))
            .Select(r => new
            {
                AthleteName = r.Attempt!.Participation.Athlete.Firstname + " " + r.Attempt.Participation.Athlete.Lastname,
                AthleteSlug = r.Attempt.Participation.Athlete.Slug,
                r.RecordCategoryId,
                WeightCategory = r.WeightCategory.Title,
                AgeCategorySlug = r.AgeCategory.Slug,
                AgeCategoryTitle = r.AgeCategory.Title,
                Gender = r.Attempt.Participation.Athlete.Gender,
                r.Weight,
                r.IsRaw,
            })
            .ToListAsync(cancellationToken);

        List<MeetRecordEntry> records = rows
            .Select(r =>
            {
                string ageCategory = DisplayNames.ResolveAgeCategoryLabel(r.AgeCategorySlug, r.AgeCategoryTitle, r.Gender);

                return new MeetRecordEntry(
                    r.AthleteName,
                    r.AthleteSlug,
                    r.RecordCategoryId.ToDisplayName(),
                    r.WeightCategory,
                    ageCategory,
                    r.Weight,
                    r.IsRaw);
            })
            .ToList();

        return records;
    }
}
