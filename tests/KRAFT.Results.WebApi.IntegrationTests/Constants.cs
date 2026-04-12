using KRAFT.Results.Tests.Shared;

namespace KRAFT.Results.WebApi.IntegrationTests;

internal static class Constants
{
    internal const string TestMeetType = TestSeedConstants.MeetType.Title;
    internal const string TestAthleteFirstName = TestSeedConstants.Athlete.FirstName;
    internal const string TestAthleteLastName = TestSeedConstants.Athlete.LastName;
    internal const string TestAthleteSlug = TestSeedConstants.Athlete.Slug;
    internal const string TestTeamSlug = TestSeedConstants.Team.Slug;
    internal const string TestCountryName = TestSeedConstants.Country.Name;
    internal const string TestMeetTitle = TestSeedConstants.Meet.Title;
    internal const string TestMeetSlug = TestSeedConstants.Meet.Slug;

    internal static readonly DateOnly TestAthleteDateOfBirth = TestSeedConstants.Athlete.DateOfBirth;

    internal static class PendingRecords
    {
        internal const int RecordBreakingAttemptId = 4;
        internal const int NonRecordBreakingAttemptId = 5;
        internal const int ApproveAttemptId = 6;
        internal const int ZeroWeightAttemptId = 7;
    }

    internal static class TeamCompetition
    {
        internal const string AlphaTeamSlug = "alpha-team";
        internal const string BetaTeamSlug = "beta-team";
        internal const string TcMeet12025Slug = "tc-meet-1-2025";
        internal const string TcMeet12026Slug = "tc-meet-1-2026";
    }

    internal static class TestUser
    {
        internal const string Username = TestSeedConstants.User.Username;
        internal const string Password = TestSeedConstants.User.Password;
        internal const string Email = TestSeedConstants.User.Email;
    }

    internal static class OrderingMeet
    {
        internal const string Slug = "ordering-meet-2025";
    }

    internal static class NoRecordsMeet
    {
        internal const int Id = 9;
        internal const string Slug = "no-records-meet";
    }

    internal static class BannedAthlete
    {
        internal const int Id = 12;
        internal const string Slug = "banned-athlete";
        internal static readonly DateOnly BanStartDate = new(2025, 1, 1);
        internal static readonly DateOnly BanEndDate = new(2025, 12, 31);
    }
}