using System.Diagnostics.CodeAnalysis;
using System.Globalization;
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
    private const string SeededCountryCode = TestSeedConstants.Country.Code;

    private const decimal SquatWeight = 200.0m;
    private const decimal BenchWeight = 130.0m;
    private const decimal DeadliftWeight = 250.0m;
    private const decimal ClassicSquatWeight = 195.0m;
    private const decimal HistoricalSquatWeight = 185.0m;
    private const decimal AthleteBodyWeight = 80.5m;
    private const int DefaultMeetTypeId = 1;
    private const int DefaultResultModeId = 1;

    private const int MaxPollAttempts = 40;
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(30);

    internal static async Task SeedAsync(
        string connectionString,
        string apiBaseUrl,
        CancellationToken cancellationToken = default)
    {
        await RunMigrationsAsync(connectionString, cancellationToken);

        await using SqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);

        await ExecuteSqlAsync(connection, BaseSeedSql.CleanupSql(), cancellationToken);
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedUsersAndRoles(), cancellationToken);
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedAgeCategories(), cancellationToken);
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedWeightCategories(), cancellationToken);
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedEras(), cancellationToken);
        await ExecuteSqlAsync(connection, BaseSeedSql.SeedEraWeightCategories(), cancellationToken);

#pragma warning disable CA5400 // Cert revocation check not needed in E2E tests
        using HttpClientHandler handler = new()
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };

        using HttpClient httpClient = new(handler, disposeHandler: false)
        {
            BaseAddress = new Uri(apiBaseUrl),
            Timeout = HttpTimeout,
        };
#pragma warning restore CA5400

        string token = await LoginAsync(httpClient, cancellationToken);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        int teamId = await CreateTeamAsync(httpClient, cancellationToken);

        await CreateAthleteAsync(httpClient, teamId, cancellationToken);

        string meetSlug = await CreateMeetAsync(
            httpClient,
            SeededMeetTitle,
            new DateOnly(SeededMeetYear, 3, 15),
            isRaw: false,
            cancellationToken);
        int meetId = await GetMeetIdAsync(httpClient, meetSlug, cancellationToken);

        int participationId = await AddParticipantAsync(httpClient, meetId, cancellationToken);

        await RecordAttemptAsync(
            httpClient,
            meetId,
            participationId,
            Discipline.Squat,
            round: 1,
            SquatWeight,
            cancellationToken);
        await RecordAttemptAsync(
            httpClient,
            meetId,
            participationId,
            Discipline.Bench,
            round: 1,
            BenchWeight,
            cancellationToken);
        await RecordAttemptAsync(
            httpClient,
            meetId,
            participationId,
            Discipline.Deadlift,
            round: 1,
            DeadliftWeight,
            cancellationToken);

        await SeedAuxiliaryMeetAsync(
            httpClient,
            "Historical Meet",
            new DateOnly(2017, 6, 15),
            isRaw: false,
            HistoricalSquatWeight,
            cancellationToken);

        await SeedAuxiliaryMeetAsync(
            httpClient,
            "Classic Meet",
            new DateOnly(SeededMeetYear, 3, 16),
            isRaw: true,
            ClassicSquatWeight,
            cancellationToken);

        await WaitForRecordsAsync(httpClient, "equipped", cancellationToken);
        await WaitForRecordsAsync(httpClient, "classic", cancellationToken);
    }

    private static async Task<string> LoginAsync(
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        LoginCommand command = new(TestSeedConstants.User.Username, TestSeedConstants.User.Password);

        HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "/users/login", command, cancellationToken);
        response.EnsureSuccessStatusCode();

        AuthenticatedResponse auth = await response.Content
            .ReadFromJsonAsync<AuthenticatedResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Login response was null.");

        return auth.AccessToken;
    }

    private static async Task<int> CreateTeamAsync(
        HttpClient httpClient,
        CancellationToken cancellationToken)
    {
        CreateTeamCommand command = new(
            Title: TestSeedConstants.Team.Title,
            TitleShort: TestSeedConstants.Team.TitleShort,
            TitleFull: TestSeedConstants.Team.TitleFull,
            CountryCode: SeededCountryCode);

        HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "/teams", command, cancellationToken);
        response.EnsureSuccessStatusCode();

        string location = response.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("POST /teams did not return a Location header.");

        return int.Parse(location.TrimStart('/'), CultureInfo.InvariantCulture);
    }

    private static async Task CreateAthleteAsync(
        HttpClient httpClient,
        int teamId,
        CancellationToken cancellationToken)
    {
        CreateAthleteCommand command = new(
            FirstName: TestSeedConstants.Athlete.FirstName,
            LastName: TestSeedConstants.Athlete.LastName,
            CountryCode: SeededCountryCode,
            TeamId: teamId,
            DateOfBirth: TestSeedConstants.Athlete.DateOfBirth,
            Gender: TestSeedConstants.Athlete.Gender);

        HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "/athletes", command, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<string> CreateMeetAsync(
        HttpClient httpClient,
        string title,
        DateOnly startDate,
        bool isRaw,
        CancellationToken cancellationToken)
    {
        CreateMeetCommand command = new(
            Title: title,
            StartDate: startDate,
            MeetTypeId: DefaultMeetTypeId,
            EndDate: startDate,
            CalcPlaces: true,
            Text: null,
            Location: null,
            PublishedResults: true,
            ResultModeId: DefaultResultModeId,
            PublishedInCalendar: true,
            IsInTeamCompetition: false,
            ShowWilks: true,
            ShowTeamPoints: false,
            ShowBodyWeight: true,
            ShowTeams: false,
            RecordsPossible: true,
            IsRaw: isRaw);

        HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            "/meets", command, cancellationToken);
        response.EnsureSuccessStatusCode();

        string location = response.Headers.Location?.ToString()
            ?? throw new InvalidOperationException("POST /meets did not return a Location header.");

        return location.TrimStart('/');
    }

    private static async Task<int> GetMeetIdAsync(
        HttpClient httpClient,
        string meetSlug,
        CancellationToken cancellationToken)
    {
        MeetDetails meetDetails = await httpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{meetSlug}", cancellationToken)
            ?? throw new InvalidOperationException($"GET /meets/{meetSlug} returned null.");

        return meetDetails.MeetId;
    }

    private static async Task<int> AddParticipantAsync(
        HttpClient httpClient,
        int meetId,
        CancellationToken cancellationToken)
    {
        AddParticipantCommand command = new(
            AthleteSlug: TestSeedConstants.Athlete.Slug,
            BodyWeight: AthleteBodyWeight,
            TeamId: null,
            AgeCategorySlug: null);

        HttpResponseMessage response = await httpClient.PostAsJsonAsync(
            $"/meets/{meetId}/participants", command, cancellationToken);
        response.EnsureSuccessStatusCode();

        AddParticipantResponse result = await response.Content
            .ReadFromJsonAsync<AddParticipantResponse>(cancellationToken)
            ?? throw new InvalidOperationException("POST /participants returned null.");

        return result.ParticipationId;
    }

    private static async Task RecordAttemptAsync(
        HttpClient httpClient,
        int meetId,
        int participationId,
        Discipline discipline,
        int round,
        decimal weight,
        CancellationToken cancellationToken)
    {
        RecordAttemptCommand command = new(weight, Good: true);

        HttpResponseMessage response = await httpClient.PutAsJsonAsync(
            $"/meets/{meetId}/participants/{participationId}/attempts/{(int)discipline}/{round}",
            command,
            cancellationToken);

        response.EnsureSuccessStatusCode();
    }

    private static async Task SeedAuxiliaryMeetAsync(
        HttpClient httpClient,
        string title,
        DateOnly startDate,
        bool isRaw,
        decimal squatWeight,
        CancellationToken cancellationToken)
    {
        string slug = await CreateMeetAsync(
            httpClient,
            title,
            startDate,
            isRaw,
            cancellationToken);
        int meetId = await GetMeetIdAsync(httpClient, slug, cancellationToken);
        int participationId = await AddParticipantAsync(httpClient, meetId, cancellationToken);
        await RecordAttemptAsync(
            httpClient,
            meetId,
            participationId,
            Discipline.Squat,
            round: 1,
            squatWeight,
            cancellationToken);
        await RecordAttemptAsync(
            httpClient,
            meetId,
            participationId,
            Discipline.Bench,
            round: 1,
            BenchWeight,
            cancellationToken);
        await RecordAttemptAsync(
            httpClient,
            meetId,
            participationId,
            Discipline.Deadlift,
            round: 1,
            DeadliftWeight,
            cancellationToken);
    }

    private static async Task WaitForRecordsAsync(
        HttpClient httpClient,
        string equipmentType,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < MaxPollAttempts; attempt++)
        {
            List<RecordGroup>? groups = await httpClient.GetFromJsonAsync<List<RecordGroup>>(
                $"/records?gender=m&ageCategory=open&equipmentType={equipmentType}",
                cancellationToken);

            bool hasRecord = groups is not null
                && groups
                    .SelectMany(g => g.Records)
                    .Any(r => r.Athlete != null);

            if (hasRecord)
            {
                return;
            }

            await Task.Delay(PollInterval, cancellationToken);
        }

        throw new TimeoutException(
            $"Records were not computed within {MaxPollAttempts * PollInterval.TotalSeconds} seconds.");
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "All SQL is composed from compile-time constants in BaseSeedSql")]
    private static async Task ExecuteSqlAsync(
        SqlConnection connection,
        string sql,
        CancellationToken cancellationToken)
    {
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task RunMigrationsAsync(
        string connectionString,
        CancellationToken cancellationToken)
    {
        DbContextOptions<ResultsDbContext> options = new DbContextOptionsBuilder<ResultsDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        await using ResultsDbContext dbContext = new(options);
        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}