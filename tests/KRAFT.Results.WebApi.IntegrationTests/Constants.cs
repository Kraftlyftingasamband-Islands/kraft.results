namespace KRAFT.Results.WebApi.IntegrationTests;

internal static class Constants
{
    internal const string TestMeetType = "Powerlifting";
    internal const string TestAthleteFirstName = "Testie";
    internal const string TestAthleteLastName = "McTestFace";
    internal const string TestAthleteSlug = "testie-mctestface";
    internal const string TestTeamSlug = "test-team";
    internal const string TestCountryName = "Iceland";

    internal static readonly DateOnly TestAthleteDateOfBirth = new(1985, 7, 2);

    internal static class TestUser
    {
        internal const string Username = "testuser";
        internal const string Password = "testuser";
        internal const string Email = "test@email.com";
    }
}