using KRAFT.Results.Contracts.Meets;

namespace KRAFT.Results.Contracts.Dashboard;

public sealed record DashboardSummary(
    DashboardSeasonStats SeasonStats,
    IReadOnlyList<MeetSummary> RecentMeets,
    IReadOnlyList<MeetSummary> UpcomingMeets,
    IReadOnlyList<DashboardRankingEntry> TopRankingsMen,
    IReadOnlyList<DashboardRankingEntry> TopRankingsWomen,
    IReadOnlyList<DashboardRecordEntry> RecentRecordsMen,
    IReadOnlyList<DashboardRecordEntry> RecentRecordsWomen,
    IReadOnlyList<DashboardTeamEntry> TeamStandingsMen,
    IReadOnlyList<DashboardTeamEntry> TeamStandingsWomen);