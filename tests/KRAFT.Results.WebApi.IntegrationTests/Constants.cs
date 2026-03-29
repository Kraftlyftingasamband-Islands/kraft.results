namespace KRAFT.Results.WebApi.IntegrationTests;

internal static class Constants
{
    internal const string TestMeetType = "Powerlifting";
    internal const string TestAthleteFirstName = "Testie";
    internal const string TestAthleteLastName = "McTestFace";
    internal const string TestAthleteSlug = "testie-mctestface";
    internal const string TestTeamSlug = "test-team";
    internal const string TestCountryName = "Iceland";
    internal const string TestMeetTitle = "Test Meet 2025";
    internal const string TestMeetSlug = "test-meet-2025";

    internal static readonly DateOnly TestAthleteDateOfBirth = new(1985, 7, 2);

    internal static class PendingRecords
    {
        internal const int RecordBreakingAttemptId = 4;
        internal const int NonRecordBreakingAttemptId = 5;
        internal const int ApproveAttemptId = 6;
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
        internal const string Username = "testuser";
        internal const string Password = "testuser";
        internal const string Email = "test@email.com";
    }

    internal static class OrderingMeet
    {
        internal const string Slug = "ordering-meet-2025";
    }
}