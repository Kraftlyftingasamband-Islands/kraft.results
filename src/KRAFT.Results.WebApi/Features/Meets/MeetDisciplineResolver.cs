using KRAFT.Results.Contracts;

namespace KRAFT.Results.WebApi.Features.Meets;

internal static class MeetDisciplineResolver
{
    private static readonly int[] BenchMeetTypeIds = [2, 5];

    internal static bool IsBenchMeetType(int meetTypeId) => BenchMeetTypeIds.Contains(meetTypeId);

    internal static IReadOnlyList<Discipline> ResolveDisciplines(int meetTypeId, string meetTypeTitle)
    {
        if (BenchMeetTypeIds.Contains(meetTypeId))
        {
            return [Discipline.Bench];
        }

        if (meetTypeTitle.Contains("réttst", StringComparison.OrdinalIgnoreCase)
            || meetTypeTitle.Contains("rettst", StringComparison.OrdinalIgnoreCase)
            || meetTypeTitle.Contains("deadlift", StringComparison.OrdinalIgnoreCase))
        {
            return [Discipline.Deadlift];
        }

        return [Discipline.Squat, Discipline.Bench, Discipline.Deadlift];
    }
}