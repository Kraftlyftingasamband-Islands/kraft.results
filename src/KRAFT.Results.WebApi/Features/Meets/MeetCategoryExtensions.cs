using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Enums;

namespace KRAFT.Results.WebApi.Features.Meets;

internal static class MeetCategoryExtensions
{
    internal static IReadOnlyList<Discipline> GetDisciplines(this MeetCategory category) => category switch
    {
        MeetCategory.Benchpress => [Discipline.Bench],
        MeetCategory.Deadlift => [Discipline.Deadlift],
        MeetCategory.PushPull => [Discipline.Bench, Discipline.Deadlift],
        _ => [Discipline.Squat, Discipline.Bench, Discipline.Deadlift],
    };

    internal static bool IsBenchCategory(this MeetCategory category) =>
        category is MeetCategory.Benchpress or MeetCategory.PushPull;

    internal static bool IsFullMeet(this MeetCategory category) =>
        category is MeetCategory.Powerlifting or MeetCategory.Squat;

    internal static bool IsSingleLiftCategory(this MeetCategory category) =>
        !category.IsFullMeet();

    internal static RecordCategory MapDisciplineToRecordCategory(
        this MeetCategory category,
        Discipline discipline) =>
        discipline switch
        {
            Discipline.Squat => RecordCategory.Squat,
            Discipline.Bench => category.IsFullMeet() ? RecordCategory.Bench : RecordCategory.BenchSingle,
            Discipline.Deadlift => category.IsFullMeet() ? RecordCategory.Deadlift : RecordCategory.DeadliftSingle,
            _ => RecordCategory.None,
        };

    internal static string ToDisplayName(this MeetCategory category) => category switch
    {
        MeetCategory.Powerlifting => Constants.Powerlifting,
        MeetCategory.Benchpress => $"{Constants.Bench} ({Constants.SingeLift})",
        MeetCategory.Deadlift => $"{Constants.Deadlift} ({Constants.SingeLift})",
        MeetCategory.Squat => $"{Constants.Squat} ({Constants.SingeLift})",
        MeetCategory.PushPull => "PushPull",
        _ => category.ToString(),
    };
}