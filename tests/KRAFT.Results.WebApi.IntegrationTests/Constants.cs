using KRAFT.Results.Tests.Shared;

namespace KRAFT.Results.WebApi.IntegrationTests;

internal static class Constants
{
    internal const string TestAthleteFirstName = TestSeedConstants.Athlete.FirstName;
    internal const string TestAthleteLastName = TestSeedConstants.Athlete.LastName;
    internal const string TestAthleteSlug = TestSeedConstants.Athlete.Slug;
    internal const string TestTeamSlug = TestSeedConstants.Team.Slug;
    internal const string TestCountryName = TestSeedConstants.Country.Name;
    internal static readonly DateOnly TestAthleteDateOfBirth = TestSeedConstants.Athlete.DateOfBirth;

    internal static class TestUser
    {
        internal const string Username = TestSeedConstants.User.Username;
        internal const string Password = TestSeedConstants.User.Password;
        internal const string Email = TestSeedConstants.User.Email;
    }
}