using KRAFT.Results.Contracts;
using KRAFT.Results.WebApi.Enums;

namespace KRAFT.Results.WebApi.Features.Meets;

internal static class MeetCategoryExtensions
{
    internal static IReadOnlyList<Discipline> GetDisciplines(this MeetCategory category) => category switch
    {
        MeetCategory.Powerlifting => [Discipline.Squat, Discipline.Bench, Discipline.Deadlift],
        MeetCategory.Benchpress => [Discipline.Bench],
        MeetCategory.Deadlift => [Discipline.Deadlift],
        MeetCategory.PushPull => [Discipline.Bench, Discipline.Deadlift],
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, $"Unsupported meet category: {category}"),
    };

    internal static bool IsBenchCategory(this MeetCategory category) =>
        category is MeetCategory.Benchpress or MeetCategory.PushPull;

    internal static RecordCategory MapDisciplineToRecordCategory(
        this MeetCategory category,
        Discipline discipline) =>
        discipline switch
        {
            Discipline.Squat => RecordCategory.Squat,
            Discipline.Bench => category is MeetCategory.Powerlifting ? RecordCategory.Bench : RecordCategory.BenchSingle,
            Discipline.Deadlift => category is MeetCategory.Powerlifting ? RecordCategory.Deadlift : RecordCategory.DeadliftSingle,
            Discipline.None => RecordCategory.None,
            _ => throw new ArgumentOutOfRangeException(nameof(discipline), discipline, $"Unsupported discipline: {discipline}"),
        };

    internal static string ToDisplayName(this MeetCategory category) => category switch
    {
        MeetCategory.Powerlifting => Constants.Powerlifting,
        MeetCategory.Benchpress => $"{Constants.Bench} ({Constants.SingeLift})",
        MeetCategory.Deadlift => $"{Constants.Deadlift} ({Constants.SingeLift})",
        MeetCategory.PushPull => Constants.PushPull,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, $"Unsupported meet category: {category}"),
    };
}