using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Enums;

namespace KRAFT.Results.WebApi.Features.Meets;

internal static class MeetDisciplineResolver
{
    internal static bool IsBenchMeetType(int meetTypeId)
    {
        MeetCategory category = (MeetCategory)meetTypeId;
        return category == MeetCategory.Benchpress || category == MeetCategory.PushPull;
    }

    internal static bool IsDeadliftMeetType(int meetTypeId, string meetTypeTitle) =>
        !IsBenchMeetType(meetTypeId)
        && ((MeetCategory)meetTypeId == MeetCategory.Deadlift
            || meetTypeTitle.Contains("réttst", StringComparison.OrdinalIgnoreCase)
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