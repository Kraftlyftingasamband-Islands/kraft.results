using KRAFT.Results.Contracts.Dashboard;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Contracts.TeamCompetition;
using KRAFT.Results.WebApi.Enums;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records;
using KRAFT.Results.WebApi.Features.TeamCompetition;
using KRAFT.Results.WebApi.ValueObjects;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.WebApi.Features.Dashboard.GetDashboard;

internal sealed class GetDashboardHandler(ResultsDbContext dbContext)
{
    public async Task<DashboardSummary> Handle(CancellationToken cancellationToken)
    {
        int currentYear = DateTime.UtcNow.Year;
        DateTime today = DateTime.UtcNow.Date;

        DashboardSeasonStats stats = await GetSeasonStatsAsync(currentYear, cancellationToken);

        List<MeetSummary> recentMeets = await dbContext.Set<Meet>()
            .Where(m => m.PublishedResults && m.StartDate <= today)
            .OrderByDescending(m => m.StartDate)
            .Take(3)
            .Select(m => new MeetSummary(m.Slug, m.Title, m.Location, DateOnly.FromDateTime(m.StartDate)))
            .ToListAsync(cancellationToken);

        List<MeetSummary> upcomingMeets = await dbContext.Set<Meet>()
            .Where(m => m.PublishedInCalendar && m.StartDate > today)
            .OrderBy(m => m.StartDate)
            .Take(3)
            .Select(m => new MeetSummary(m.Slug, m.Title, m.Location, DateOnly.FromDateTime(m.StartDate)))
            .ToListAsync(cancellationToken);

        List<DashboardRankingEntry> rankingsMen = await GetTopRankingsAsync("m", currentYear, cancellationToken);
        List<DashboardRankingEntry> rankingsWomen = await GetTopRankingsAsync("f", currentYear, cancellationToken);

        List<DashboardRecordEntry> recordsMen = await GetRecentRecordsAsync("m", cancellationToken);
        List<DashboardRecordEntry> recordsWomen = await GetRecentRecordsAsync("f", cancellationToken);

        (List<TeamCompetitionStanding> teamsMen, List<TeamCompetitionStanding> teamsWomen) =
            await GetTeamStandingsAsync(currentYear, cancellationToken);

        return new DashboardSummary(
            stats,
            recentMeets,
            upcomingMeets,
            rankingsMen,
            rankingsWomen,
            recordsMen,
            recordsWomen,
            teamsMen,
            teamsWomen);
    }

    private async Task<DashboardSeasonStats> GetSeasonStatsAsync(int year, CancellationToken cancellationToken)
    {
        int meets = await dbContext.Set<Meet>()
            .Where(m => m.PublishedResults && m.StartDate.Year == year)
            .CountAsync(cancellationToken);

        int athletes = await dbContext.Set<Participation>()
            .Where(p => p.Meet.PublishedResults && p.Meet.StartDate.Year == year)
            .Where(p => !p.Disqualified && p.Total > 0)
            .Select(p => p.AthleteId)
            .Distinct()
            .CountAsync(cancellationToken);

        int records = await dbContext.Set<Record>()
            .Where(r => r.AttemptId != null && r.Date.Year == year)
            .CountAsync(cancellationToken);

        int clubs = await dbContext.Set<Participation>()
            .Where(p => p.Meet.PublishedResults && p.Meet.StartDate.Year == year && p.TeamId != null)
            .Select(p => p.TeamId)
            .Distinct()
            .CountAsync(cancellationToken);

        return new DashboardSeasonStats(meets, athletes, records, clubs);
    }

    private async Task<List<DashboardRankingEntry>> GetTopRankingsAsync(
        string gender, int year, CancellationToken cancellationToken)
    {
        List<RawRankingRow> rows = await dbContext.Set<Participation>()
            .Where(p => !p.Disqualified)
            .Where(p => p.Athlete.Country.Iso2 == "IS")
            .Where(p => p.Meet.IsRaw)
            .Where(p => p.Meet.MeetType.MeetTypeId == 1)
            .Where(p => p.Meet.StartDate.Year == year)
            .Where(p => p.Total > 0)
            .Where(p => p.Athlete.Gender == Gender.Parse(gender))
            .Select(p => new RawRankingRow(
                p.AthleteId,
                p.Athlete.Firstname + " " + p.Athlete.Lastname,
                p.Athlete.Slug,
                p.Total,
                p.WeightCategory.Title,
                p.Weight,
                p.Meet.IsRaw))
            .ToListAsync(cancellationToken);

        Gender parsedGender = Gender.Parse(gender);

        return rows
            .Select(r => new
            {
                r,
                IpfPoints = IpfPoints.Create(r.IsRaw, parsedGender, "Powerlifting", r.BodyWeight, r.Total).Value,
            })
            .GroupBy(x => x.r.AthleteId)
            .Select(g => g.OrderByDescending(x => x.IpfPoints).First())
            .OrderByDescending(x => x.IpfPoints)
            .Take(3)
            .Select(x => new DashboardRankingEntry(
                x.r.AthleteSlug,
                x.r.AthleteName,
                x.r.WeightCategory,
                x.IpfPoints))
            .ToList();
    }

    private async Task<List<DashboardRecordEntry>> GetRecentRecordsAsync(
        string gender, CancellationToken cancellationToken)
    {
        List<RawRecordRow> rows = await dbContext.Set<Record>()
            .Where(r => r.AttemptId != null)
            .Where(r => r.RecordCategoryId != RecordCategory.TotalWilks
                     && r.RecordCategoryId != RecordCategory.TotalIpfPoints)
            .Where(r => r.Attempt!.Participation.Athlete.Gender == Gender.Parse(gender))
            .OrderByDescending(r => r.Date)
            .Take(3)
            .Select(r => new RawRecordRow(
                r.RecordCategoryId,
                r.Attempt!.Participation.Athlete.Slug,
                r.Attempt.Participation.Athlete.Firstname + " " + r.Attempt.Participation.Athlete.Lastname,
                r.WeightCategory.Title,
                r.AgeCategory.Slug ?? string.Empty,
                r.IsRaw,
                r.Weight,
                r.Attempt.Participation.Meet.Slug,
                r.Date))
            .ToListAsync(cancellationToken);

        return rows.Select(r => new DashboardRecordEntry(
            r.RecordCategoryId.ToDisplayName(),
            r.AthleteSlug,
            r.AthleteName,
            r.WeightCategory,
            r.AgeCategory,
            r.IsRaw,
            r.Weight,
            r.MeetSlug,
            r.Date))
            .ToList();
    }

    private async Task<(List<TeamCompetitionStanding> Men, List<TeamCompetitionStanding> Women)> GetTeamStandingsAsync(
        int year, CancellationToken cancellationToken)
    {
        int bestN = TeamStandingsBuilder.GetBestN(year);

        List<TeamStandingsBuilder.TeamPointRow> rows = await dbContext.Set<Participation>()
            .Where(p => !p.Disqualified)
            .Where(p => p.Meet.IsInTeamCompetition)
            .Where(p => p.Meet.StartDate.Year == year)
            .Where(p => p.TeamId != null)
            .Where(p => p.TeamPoints != null && p.TeamPoints > 0)
            .Select(p => new TeamStandingsBuilder.TeamPointRow(
                p.TeamId!.Value,
                p.Team!.Title,
                p.Team.TitleShort,
                p.Team.Slug,
                p.Team.LogoImageFilename,
                p.Athlete.Gender.Value,
                p.MeetId,
                p.TeamPoints!.Value))
            .ToListAsync(cancellationToken);

        List<TeamCompetitionStanding> men = TeamStandingsBuilder
            .BuildStandings(rows.Where(r => r.Gender == "m"), bestN)
            .Take(3)
            .ToList();

        List<TeamCompetitionStanding> women = TeamStandingsBuilder
            .BuildStandings(rows.Where(r => r.Gender == "f"), bestN)
            .Take(3)
            .ToList();

        return (men, women);
    }

    private sealed record RawRankingRow(
        int AthleteId,
        string AthleteName,
        string AthleteSlug,
        decimal Total,
        string WeightCategory,
        decimal BodyWeight,
        bool IsRaw);

    private sealed record RawRecordRow(
        RecordCategory RecordCategoryId,
        string AthleteSlug,
        string AthleteName,
        string WeightCategory,
        string AgeCategory,
        bool IsRaw,
        decimal Weight,
        string MeetSlug,
        DateOnly Date);
}