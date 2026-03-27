using KRAFT.Results.Contracts;

namespace KRAFT.Results.WebApi.Enums;

internal static class RecordCategoryExtensions
{
    public static string ToDisplayName(this RecordCategory category) => category switch
    {
        RecordCategory.Squat => Constants.Squat,
        RecordCategory.Bench => Constants.Bench,
        RecordCategory.Deadlift => Constants.Deadlift,
        RecordCategory.Total => Constants.Total,
        RecordCategory.BenchSingle => $"{Constants.Bench} ({Constants.SingeLift})",
        RecordCategory.DeadliftSingle => $"{Constants.Deadlift} ({Constants.SingeLift})",
        _ => string.Empty,
    };
}