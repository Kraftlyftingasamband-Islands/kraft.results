using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Contracts.Records;
using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.Contracts.Users;
using KRAFT.Results.Tests.Shared;
using KRAFT.Results.WebApi;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.Web.E2ETests;

internal static class TestDataSeeder
{
    public const int SeededMeetYear = TestSeedConstants.Meet.Year;
    public const string SeededMeetSlug = TestSeedConstants.Meet.Slug;

    private const string SeededMeetTitle = TestSeedConstants.Meet.Title;
    private const int SeededCountryId = TestSeedConstants.Country.Id;

    private const decimal SquatWeight = 200.0m;
    private const decimal BenchWeight = 130.0m;
    private const decimal DeadliftWeight = 250.0m;
    private const decimal AthleteBodyWeight = 80.5m;

    private const int MaxPollAttempts = 40;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(30);

    internal static async Task SeedAsync(string connectionString, string apiBaseUrl)
    {
        await RunMigrationsAsync(connectionString);

        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync();

        await ExecuteSqlAsync(connection, BaseSeedSql.CleanupSql());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedCountry());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedUsersAndRoles());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedAgeCategories());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedWeightCategories());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedEras());
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedEraWeightCategories());

        using HttpClientHandler handler = new()
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };

#pragma warning disable CA5400 // Cert revocation check not needed in E2E tests
        using HttpClient httpClient = new(handler, disposeHandler: false)
        {
            BaseAddress = new Uri(apiBaseUrl),
            Timeout = HttpTimeout,
        };
#pragma warning restore CA5400

        string token = await LoginAsync(httpClient);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        int teamId = await CreateTeamAsync(httpClient);

        await CreateAthleteAsync(httpClient, teamId);

        string meetSlug = await CreateMeetAsync(httpClient);
        int meetId = await GetMeetIdAsync(httpClient, meetSlug);

        int participationId = await AddParticipantAsync(httpClient, meetId);

        await RecordAttemptAsync(httpClient, meetId, participationId, Discipline.Squat, round: 1, SquatWeight);
        await RecordAttemptAsync(httpClient, meetId, participationId, Discipline.Bench, round: 1, BenchWeight);
        await RecordAttemptAsync(httpClient, meetId, participationId, Discipline.Deadlift, round: 1, DeadliftWeight);

        await WaitForRecordsAsync(httpClient);
    }

    private static async Task<string> LoginAsync(HttpClient httpClient)
    {
        LoginCommand command = new(TestSeedConstants.User.Username, TestSeedConstants.User.Password);
        HttpResponseMessage response = await httpClient.PostAsJsonAsync("/users/login", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        AuthenticatedResponse? auth = await response.Content.ReadFromJsonAsync<AuthenticatedResponse>(CancellationToken.None);
        return auth!.AccessToken;
    }

    private static async Task<int> CreateTeamAsync(HttpClient httpClient)
    {
        CreateTeamCommand command = new(
            Title: TestSeedConstants.Team.Title,
            TitleShort: TestSeedConstants.Team.TitleShort,
            TitleFull: TestSeedConstants.Team.TitleFull,
            CountryId: SeededCountryId);

        HttpResponseMessage response = await httpClient.PostAsJsonAsync("/teams", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        TeamIdResponse? result = await response.Content.ReadFromJsonAsync<TeamIdResponse>(CancellationToken.None);
        return result!.TeamId;
    }

    private static async Task CreateAthleteAsync(HttpClient httpClient, int teamId)
    {
        CreateAthleteCommand command = new(
            FirstName: TestSeedConstants.Athlete.FirstName,
            LastName: TestSeedConstants.Athlete.LastName,
            CountryId: SeededCountryId,
            TeamId: teamId,
            DateOfBirth: TestSeedConstants.Athlete.DateOfBirth,
            Gender: TestSeedConstants.Athlete.Gender);

        HttpResponseMessage response = await httpClient.PostAsJsonAsync("/athletes", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<string> CreateMeetAsync(HttpClient httpClient)
    {
        CreateMeetCommand command = new(
            Title: SeededMeetTitle,
            StartDate: new DateOnly(SeededMeetYear, 3, 15),
            MeetTypeId: 1,
            EndDate: new DateOnly(SeededMeetYear, 3, 15),
            CalcPlaces: true,
            Text: null,
            Location: null,
            PublishedResults: true,
            ResultModeId: 1,
            PublishedInCalendar: true,
            IsInTeamCompetition: false,
            ShowWilks: true,
            ShowTeamPoints: false,
            ShowBodyWeight: true,
            ShowTeams: false,
            RecordsPossible: true,
            IsRaw: false);

        HttpResponseMessage response = await httpClient.PostAsJsonAsync("/meets", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        string slug = response.Headers.Location!.ToString().TrimStart('/');
        return slug;
    }

    private static async Task<int> GetMeetIdAsync(HttpClient httpClient, string meetSlug)
    {
        MeetDetails? meetDetails = await httpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{meetSlug}", CancellationToken.None);

        return meetDetails!.MeetId;
    }

    private static async Task<int> AddParticipantAsync(HttpClient httpClient, int meetId)
    {
        AddParticipantCommand command = new(
            AthleteSlug: TestSeedConstants.Athlete.Slug,
            BodyWeight: AthleteBodyWeight,
            TeamId: null,
            AgeCategorySlug: null);

        HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            $"/meets/{meetId}/participants", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        AddParticipantResponse? result = await response.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        return result!.ParticipationId;
    }

    private static async Task RecordAttemptAsync(
        HttpClient httpClient,
        int meetId,
        int participationId,
        Discipline discipline,
        int round,
        decimal weight)
    {
        RecordAttemptCommand command = new(weight, Good: true);

        HttpResponseMessage response = await httpClient.PutAsJsonAsync(
            $"/meets/{meetId}/participants/{participationId}/attempts/{(int)discipline}/{round}",
            command,
            CancellationToken.None);

        response.EnsureSuccessStatusCode();
    }

    private static async Task WaitForRecordsAsync(HttpClient httpClient)
    {
        for (int attempt = 0; attempt < MaxPollAttempts; attempt++)
        {
            List<RecordGroup>? groups = await httpClient.GetFromJsonAsync<List<RecordGroup>>(
                "/records?gender=m&ageCategory=open&equipmentType=equipped",
                CancellationToken.None);

            bool hasRecord = groups is not null
                && groups.SelectMany(g => g.Records).Any(r => r.Athlete != null);

            if (hasRecord)
            {
                return;
            }

            await Task.Delay(PollInterval);
        }
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "All SQL is composed from compile-time constants in BaseSeedSql")]
    private static async Task ExecuteSqlAsync(SqlConnection connection, string sql)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static async Task RunMigrationsAsync(string connectionString)
    {
        DbContextOptions<ResultsDbContext> options = new DbContextOptionsBuilder<ResultsDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using ResultsDbContext dbContext = new(options);
        await dbContext.Database.MigrateAsync();
    }

    private sealed record TeamIdResponse(int TeamId);
}