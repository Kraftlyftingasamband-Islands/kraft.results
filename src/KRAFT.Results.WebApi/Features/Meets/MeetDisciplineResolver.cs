using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Enums;

namespace KRAFT.Results.WebApi.Features.Meets;

internal static class MeetDisciplineResolver
{
    private static readonly int[] BenchMeetTypeIds = [2, 5];

    internal static bool IsBenchMeetType(int meetTypeId) => BenchMeetTypeIds.Contains(meetTypeId);

    internal static bool IsDeadliftMeetType(int meetTypeId, string meetTypeTitle) =>
        !IsBenchMeetType(meetTypeId)
        && (meetTypeTitle.Contains("réttst", StringComparison.OrdinalIgnoreCase)
            || meetTypeTitle.Contains("rettst", StringComparison.OrdinalIgnoreCase)
            || meetTypeTitle.Contains("deadlift", StringComparison.OrdinalIgnoreCase));

    internal static IReadOnlyList<Discipline> ResolveDisciplines(int meetTypeId, string meetTypeTitle)
    {
        if (IsBenchMeetType(meetTypeId))
        {
            return [Discipline.Bench];
        }

        if (IsDeadliftMeetType(meetTypeId, meetTypeTitle))
        {
            return [Discipline.Deadlift];
        }

        return [Discipline.Squat, Discipline.Bench, Discipline.Deadlift];
    }

    internal static RecordCategory MapDisciplineToRecordCategory(
        Discipline discipline,
        int meetTypeId,
        string meetTypeTitle)
    {
        return discipline switch
        {
            Discipline.Squat => RecordCategory.Squat,
            Discipline.Bench => IsBenchMeetType(meetTypeId)
                ? RecordCategory.BenchSingle
                : RecordCategory.Bench,
            Discipline.Deadlift => IsDeadliftMeetType(meetTypeId, meetTypeTitle)
                ? RecordCategory.DeadliftSingle
                : RecordCategory.Deadlift,
            _ => RecordCategory.None,
        };
    }
}