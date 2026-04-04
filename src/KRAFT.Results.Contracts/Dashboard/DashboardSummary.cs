using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Contracts.Rankings;
using KRAFT.Results.Contracts.TeamCompetition;

namespace KRAFT.Results.Contracts.Dashboard;

public sealed record DashboardSummary(
    DashboardSeasonStats SeasonStats,
    IReadOnlyList<MeetSummary> RecentMeets,
    IReadOnlyList<MeetSummary> UpcomingMeets,
    IReadOnlyList<RankingEntry> TopRankingsMen,
    IReadOnlyList<RankingEntry> TopRankingsWomen,
    IReadOnlyList<DashboardRecordEntry> RecentRecordsMen,
    IReadOnlyList<DashboardRecordEntry> RecentRecordsWomen,
    IReadOnlyList<TeamCompetitionStanding> TeamStandingsMen,
    IReadOnlyList<TeamCompetitionStanding> TeamStandingsWomen);