using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.Features.Records.ComputeRecords;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Athletes;

[Collection(nameof(GetAthletePersonalBestsTestsCollection))]
public sealed class GetAthletePersonalBestsTests(CollectionFixture fixture) : IAsyncLifetime
{
    // Squat PB weight constants
    private const decimal SquatWeight = 200.0m;
    private const decimal BenchWeight = 130.0m;
    private const decimal DeadliftWeight = 250.0m;

    // Open athlete — born 1990, age 35 at test meets → resolves to "open" only
    private static readonly DateOnly OpenAthleteDob = new(1990, 6, 15);

    // Junior athlete — born 2003, age 22 at test meets → resolves to "junior", cascades to ["junior", "open"]
    private static readonly DateOnly JuniorAthleteDob = new(2003, 1, 1);

    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();
    private readonly string _suffix = UniqueShortCode.Next();
    private readonly List<string> _athleteSlugs = [];
    private readonly List<string> _meetSlugs = [];
    private readonly List<(int MeetId, int ParticipationId)> _participations = [];
    private HttpClient _authorizedHttpClient = null!;
    private RecordComputationChannel _channel = null!;

    private string _openAthleteSlug = string.Empty;
    private string _juniorAthleteSlug = string.Empty;
    private int _equippedMeetId;
    private int _juniorEquippedMeetId;
    private int _noRecordMeetId;
    private string _noRecordAthleteSlug = string.Empty;

    public async ValueTask InitializeAsync()
    {
        (_authorizedHttpClient, _channel) = fixture.CreateAuthorizedHttpClientWithRecordComputation();

        _openAthleteSlug = await CreateAthleteAsync("PbOpen", "m", OpenAthleteDob);
        _juniorAthleteSlug = await CreateAthleteAsync("PbJunior", "m", JuniorAthleteDob);
        _noRecordAthleteSlug = await CreateAthleteAsync("PbNoRec", "m", OpenAthleteDob);

        // Full powerlifting equipped meet — open athlete sets squat/bench/deadlift records
        _equippedMeetId = await CreateMeetAndGetIdAsync(new DateOnly(2025, 3, 15), isRaw: false);

        // Full powerlifting equipped meet — junior athlete participates (cascade: junior + open)
        _juniorEquippedMeetId = await CreateMeetAndGetIdAsync(new DateOnly(2025, 5, 1), isRaw: false);

        // Full powerlifting equipped meet — no-record athlete with lower lifts, no current records
        _noRecordMeetId = await CreateMeetAndGetIdAsync(new DateOnly(2025, 6, 1), isRaw: false);

        // Open athlete: 83 kg bodyweight → 83 kg weight category
        int p1 = await AddParticipantAsync(_equippedMeetId, _openAthleteSlug, 80.5m);
        _participations.Add((_equippedMeetId, p1));
        await RecordAttemptAsync(_equippedMeetId, p1, Discipline.Squat, 1, SquatWeight);
        await RecordAttemptAsync(_equippedMeetId, p1, Discipline.Bench, 1, BenchWeight);
        await RecordAttemptAsync(_equippedMeetId, p1, Discipline.Deadlift, 1, DeadliftWeight);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // Junior athlete: 90 kg bodyweight → 93 kg category, which is uncontested (the open
        // athlete competes at 83 kg). An uncontested category lets the junior's lift win both
        // the junior slot and — via the age cascade — the open slot, producing junior + open
        // records per discipline. In a contested category the higher open lift would keep the
        // open slot, leaving the junior only the junior slot.
        int p2 = await AddParticipantAsync(_juniorEquippedMeetId, _juniorAthleteSlug, 90.0m);
        _participations.Add((_juniorEquippedMeetId, p2));
        await RecordAttemptAsync(_juniorEquippedMeetId, p2, Discipline.Squat, 1, 180.0m);
        await RecordAttemptAsync(_juniorEquippedMeetId, p2, Discipline.Bench, 1, 100.0m);
        await RecordAttemptAsync(_juniorEquippedMeetId, p2, Discipline.Deadlift, 1, 200.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);

        // No-record athlete: same weight category (83 kg) but lower lifts → no current records
        int p3 = await AddParticipantAsync(_noRecordMeetId, _noRecordAthleteSlug, 80.5m);
        _participations.Add((_noRecordMeetId, p3));
        await RecordAttemptAsync(_noRecordMeetId, p3, Discipline.Squat, 1, 100.0m);
        await RecordAttemptAsync(_noRecordMeetId, p3, Discipline.Bench, 1, 70.0m);
        await RecordAttemptAsync(_noRecordMeetId, p3, Discipline.Deadlift, 1, 120.0m);
        await _channel.WaitUntilDrainedAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        foreach ((int meetId, int participationId) in _participations)
        {
            await _authorizedHttpClient.DeleteAsync(
                $"/meets/{meetId}/participants/{participationId}", CancellationToken.None);
        }

        foreach (string slug in _meetSlugs)
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{slug}", CancellationToken.None);
        }

        foreach (string slug in _athleteSlugs)
        {
            await _authorizedHttpClient.DeleteAsync($"/athletes/{slug}", CancellationToken.None);
        }

        _authorizedHttpClient.Dispose();
        _httpClient.Dispose();
    }

    [Fact]
    public async Task WhenAthleteExists_ReturnsOk()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"/athletes/{_openAthleteSlug}/personalbests",
            TestContext.Current.CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenLiftPbHoldsOpenRecord_MatchesContainsThatSlot()
    {
        // Arrange

        // Act
        List<AthletePersonalBest>? personalBests = await _httpClient.GetFromJsonAsync<List<AthletePersonalBest>>(
            $"/athletes/{_openAthleteSlug}/personalbests",
            TestContext.Current.CancellationToken);

        // Assert
        personalBests.ShouldNotBeNull();
        personalBests.ShouldContain(pb => pb.Discipline == Discipline.Squat);
        AthletePersonalBest squatPb = personalBests.Single(pb => pb.Discipline == Discipline.Squat);
        squatPb.Weight.ShouldBe(SquatWeight);
        squatPb.Matches.ShouldNotBeEmpty();
        squatPb.Matches.ShouldContain(m => m.AgeCategory == "Opinn flokkur" && m.WeightCategory == "83");
    }

    [Fact]
    public async Task WhenTotalPbHoldsRecord_MatchesCorrelateViaParticipation()
    {
        // Arrange

        // Act
        List<AthletePersonalBest>? personalBests = await _httpClient.GetFromJsonAsync<List<AthletePersonalBest>>(
            $"/athletes/{_openAthleteSlug}/personalbests",
            TestContext.Current.CancellationToken);

        // Assert
        personalBests.ShouldNotBeNull();
        personalBests.ShouldContain(pb => pb.Discipline == Discipline.None);
        AthletePersonalBest totalPb = personalBests.Single(pb => pb.Discipline == Discipline.None);
        totalPb.Matches.ShouldNotBeEmpty();
        totalPb.Matches.ShouldContain(m => m.AgeCategory == "Opinn flokkur" && m.WeightCategory == "83");
        AthleteRecordMatch totalMatch = totalPb.Matches.Single(m => m.AgeCategory == "Opinn flokkur");
        totalMatch.ShouldSatisfyAllConditions(
            () => totalMatch.WeightCategory.ShouldBe("83"),
            () => totalMatch.IsWithinPowerlifting.ShouldBeFalse(),
            () => totalMatch.IsSingleLift.ShouldBeFalse(),
            () => totalMatch.IsStandaloneDiscipline.ShouldBeFalse());
    }

    [Fact]
    public async Task WhenJuniorAthleteLifts_PbMatchesCascadeToJuniorAndOpen()
    {
        // Arrange
        // Junior athlete born 2003, age 22 at 2025-05-01 → "junior" cascades to ["junior", "open"]

        // Act
        List<AthletePersonalBest>? personalBests = await _httpClient.GetFromJsonAsync<List<AthletePersonalBest>>(
            $"/athletes/{_juniorAthleteSlug}/personalbests",
            TestContext.Current.CancellationToken);

        // Assert
        personalBests.ShouldNotBeNull();
        personalBests.ShouldContain(pb => pb.Discipline == Discipline.Squat);
        AthletePersonalBest squatPb = personalBests.Single(pb => pb.Discipline == Discipline.Squat);
        squatPb.Matches.Count.ShouldBeGreaterThanOrEqualTo(2);
        squatPb.Matches.ShouldContain(m => m.AgeCategory == "Unglingaflokkur");
        squatPb.Matches.ShouldContain(m => m.AgeCategory == "Opinn flokkur");
    }

    [Fact]
    public async Task WhenAthleteHasPbsButNoRecords_AllMatchesEmpty()
    {
        // Arrange

        // Act
        List<AthletePersonalBest>? personalBests = await _httpClient.GetFromJsonAsync<List<AthletePersonalBest>>(
            $"/athletes/{_noRecordAthleteSlug}/personalbests",
            TestContext.Current.CancellationToken);

        // Assert
        personalBests.ShouldNotBeNull();
        personalBests.ShouldNotBeEmpty();
        foreach (AthletePersonalBest pb in personalBests)
        {
            pb.Matches.ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task WhenBenchPbInPowerliftingMeet_IsWithinPowerliftingFlagSet()
    {
        // Arrange
        // A bench attempt in a full powerlifting meet also creates a BenchSingle slot
        // with IsWithinPowerlifting = true (RecordCategory is BenchSingle but meet is Powerlifting)

        // Act
        List<AthletePersonalBest>? personalBests = await _httpClient.GetFromJsonAsync<List<AthletePersonalBest>>(
            $"/athletes/{_openAthleteSlug}/personalbests",
            TestContext.Current.CancellationToken);

        // Assert
        personalBests.ShouldNotBeNull();
        personalBests.ShouldContain(pb => pb.Discipline == Discipline.Bench);
        AthletePersonalBest benchPb = personalBests.Single(pb => pb.Discipline == Discipline.Bench);
        benchPb.Matches.ShouldContain(m => m.IsWithinPowerlifting && m.AgeCategory == "Opinn flokkur");
        AthleteRecordMatch withinPowerliftingMatch = benchPb.Matches.Single(m => m.IsWithinPowerlifting);
        withinPowerliftingMatch.IsSingleLift.ShouldBeFalse();
    }

    private async Task<string> CreateAthleteAsync(string prefix, string gender, DateOnly dateOfBirth)
    {
        string firstName = $"{prefix}{_suffix}";
        string lastName = "Pb";

        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithFirstName(firstName)
            .WithLastName(lastName)
            .WithGender(gender)
            .WithDateOfBirth(dateOfBirth)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/athletes", command, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        string slug = Slug.Create($"{firstName} {lastName}");
        _athleteSlugs.Add(slug);
        return slug;
    }

    private async Task<int> CreateMeetAndGetIdAsync(DateOnly startDate, bool isRaw)
    {
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(startDate)
            .WithIsRaw(isRaw)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            "/meets", command, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        string slug = response.Headers.Location!.ToString().TrimStart('/');
        _meetSlugs.Add(slug);

        MeetDetails? meetDetails = await _authorizedHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{slug}", TestContext.Current.CancellationToken);

        return meetDetails!.MeetId;
    }

    private async Task<int> AddParticipantAsync(int meetId, string athleteSlug, decimal bodyWeight)
    {
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .WithBodyWeight(bodyWeight)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PostAsJsonAsync(
            $"/meets/{meetId}/participants", command, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();

        AddParticipantResponse? result = await response.Content
            .ReadFromJsonAsync<AddParticipantResponse>(TestContext.Current.CancellationToken);

        return result!.ParticipationId;
    }

    private async Task RecordAttemptAsync(
        int meetId,
        int participationId,
        Discipline discipline,
        int round,
        decimal weight)
    {
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(weight)
            .Build();

        HttpResponseMessage response = await _authorizedHttpClient.PutAsJsonAsync(
            $"/meets/{meetId}/participants/{participationId}/attempts/{(int)discipline}/{round}",
            command,
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();
    }
}
