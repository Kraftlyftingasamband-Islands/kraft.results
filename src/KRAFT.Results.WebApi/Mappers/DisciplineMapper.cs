using KRAFT.Results.Contracts;

namespace KRAFT.Results.WebApi.Mappers;

internal static class DisciplineMapper
{
#pragma warning disable S3358 // Ternary operators should not be nested
    public static string Map(int disciplineId) =>
        IsSquat(disciplineId) ? Constants.Squat
        : IsBench(disciplineId) ? Constants.Bench
        : IsDeadlift(disciplineId) ? Constants.Deadlift
        : Constants.Total;
#pragma warning restore S3358 // Ternary operators should not be nested

    private static bool IsSquat(int disciplineId) => disciplineId == (byte)Discipline.Squat;

    private static bool IsBench(int disciplineId) => disciplineId == (byte)Discipline.Bench;

    private static bool IsDeadlift(int disciplineId) => disciplineId == (byte)Discipline.Deadlift;
}