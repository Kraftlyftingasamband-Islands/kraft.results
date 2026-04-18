using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;
using KRAFT.Results.WebApi.ValueObjects;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(MeetsCollection))]
public sealed class GetMeetParticipationsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private readonly HttpClient _setupHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();
    private readonly string _suffix = UniqueShortCode.Next();
    private readonly List<string> _athleteSlugs = [];
    private int _meetId;
    private string _meetSlug = string.Empty;
    private string _charlieFullName = string.Empty;
    private string _deltaFullName = string.Empty;
    private string _bobFullName = string.Empty;
    private string _annaFullName = string.Empty;
    private int _charlieParticipationId;
    private int _deltaParticipationId;
    private int _bobParticipationId;
    private int _annaParticipationId;

    public async ValueTask InitializeAsync()
    {
        CreateMeetCommand meetCommand = new CreateMeetCommandBuilder()
            .WithIsRaw(true)
            .Build();

        HttpResponseMessage createResponse = await _setupHttpClient.PostAsJsonAsync(
            "/meets", meetCommand, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();

        _meetSlug = createResponse.Headers.Location!.ToString().TrimStart('/');

        MeetDetails? details = await _setupHttpClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{_meetSlug}", CancellationToken.None);
        _meetId = details!.MeetId;

        string charlieSlug = await CreateAthleteAsync("Charlie", $"Test{_suffix}");
        string deltaSlug = await CreateAthleteAsync("Delta", $"Test{_suffix}");
        string bobSlug = await CreateAthleteAsync("Bob", $"Test{_suffix}");
        string annaSlug = await CreateAthleteAsync("Anna", $"Test{_suffix}");

        _charlieFullName = $"Charlie Test{_suffix}";
        _deltaFullName = $"Delta Test{_suffix}";
        _bobFullName = $"Bob Test{_suffix}";
        _annaFullName = $"Anna Test{_suffix}";

        _charlieParticipationId = await AddParticipantAsync(charlieSlug);
        _deltaParticipationId = await AddParticipantAsync(deltaSlug);
        _bobParticipationId = await AddParticipantAsync(bobSlug);
        _annaParticipationId = await AddParticipantAsync(annaSlug);

        await RecordFullTotalAsync(_charlieParticipationId, 195.0m, 125.0m, 250.0m);
        await RecordFullTotalAsync(_deltaParticipationId, 195.0m, 125.0m, 250.0m);
        await RecordFullTotalAsync(_bobParticipationId, 170.0m, 110.0m, 220.0m);

        await RecordAttempt(_annaParticipationId, Discipline.Squat, 1, 100.0m, false);
        await RecordAttempt(_annaParticipationId, Discipline.Squat, 2, 100.0m, false);
        await RecordAttempt(_annaParticipationId, Discipline.Squat, 3, 100.0m, false);

        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET Place = 1 WHERE ParticipationId IN ({_charlieParticipationId}, {_deltaParticipationId})");
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET Place = 3 WHERE ParticipationId = {_bobParticipationId}");
        await fixture.ExecuteSqlAsync(
            $"UPDATE Participations SET Disqualified = 1 WHERE ParticipationId = {_annaParticipationId}");
    }

    public async ValueTask DisposeAsync()
    {
        if (_meetId != 0)
        {
            await _setupHttpClient.DeleteAsync($"/meets/{_meetSlug}", CancellationToken.None);
        }

        foreach (string slug in _athleteSlugs)
        {
            await _setupHttpClient.DeleteAsync($"/athletes/{slug}", CancellationToken.None);
        }

        _setupHttpClient.Dispose();
        _httpClient.Dispose();
    }

    [Fact]
    public async Task ReturnsOk()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"/meets/{_meetSlug}/participations", CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PlacedParticipantsAppearBeforeDisqualified()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(
            $"/meets/{_meetSlug}/participations", CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        participations.Count.ShouldBe(4);

        List<MeetParticipation> placed = participations
            .Where(p => p.Rank > 0)
            .ToList();
        List<MeetParticipation> unplaced = participations
            .Where(p => p.Rank <= 0)
            .ToList();

        int lastPlacedIndex = participations.IndexOf(placed[^1]);
        int firstUnplacedIndex = participations.IndexOf(unplaced[0]);

        lastPlacedIndex.ShouldBeLessThan(firstUnplacedIndex);
    }

    [Fact]
    public async Task DisqualifiedParticipantAppearsLast()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(
            $"/meets/{_meetSlug}/participations", CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation last = participations[^1];
        last.Disqualified.ShouldBeTrue();
        last.Athlete.ShouldBe(_annaFullName);
    }

    [Fact]
    public async Task ParticipantsWithSameRankAreSortedAlphabetically()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(
            $"/meets/{_meetSlug}/participations", CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        List<MeetParticipation> tiedAtFirst = participations
            .Where(p => p.Rank == 1)
            .ToList();
        tiedAtFirst.Count.ShouldBe(2);
        tiedAtFirst[0].Athlete.ShouldBe(_charlieFullName);
        tiedAtFirst[1].Athlete.ShouldBe(_deltaFullName);
    }

    [Fact]
    public async Task ParticipationsAreOrderedByRankAscending()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(
            $"/meets/{_meetSlug}/participations", CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        List<MeetParticipation> placed = participations
            .Where(p => p.Rank > 0)
            .ToList();
        List<int> ranks = placed
            .Select(p => p.Rank)
            .ToList();
        ranks.ShouldBe(ranks.OrderBy(r => r).ToList());
    }

    [Fact]
    public async Task DisqualifiedFieldIsTrueForDisqualifiedParticipant()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(
            $"/meets/{_meetSlug}/participations", CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation disqualified = participations.Single(p => p.Athlete == _annaFullName);
        disqualified.Disqualified.ShouldBeTrue();
    }

    [Fact]
    public async Task AgeCategoryIsTranslatedToIcelandic()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(
            $"/meets/{_meetSlug}/participations", CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation participation = participations.First(p => p.Athlete == _charlieFullName);
        participation.AgeCategory.ShouldBe("Opinn flokkur");
        participation.AgeCategorySlug.ShouldBe("open");
    }

    [Fact]
    public async Task DisqualifiedFieldIsFalseForPlacedParticipant()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(
            $"/meets/{_meetSlug}/participations", CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation placed = participations.First(p => p.Athlete == _bobFullName);
        placed.Disqualified.ShouldBeFalse();
    }

    [Fact]
    public async Task NonDisqualifiedParticipant_IpfPointsAreCalculatedFromTotal()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(
            $"/meets/{_meetSlug}/participations", CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation participation = participations.First(p => p.Athlete == _deltaFullName);
        participation.IpfPoints.ShouldBeGreaterThan(78m);
        participation.IpfPoints.ShouldBeLessThan(83m);
    }

    [Fact]
    public async Task DisqualifiedParticipant_IpfPointsAreZero()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(
            $"/meets/{_meetSlug}/participations", CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation participation = participations.Single(p => p.Athlete == _annaFullName);
        participation.IpfPoints.ShouldBe(0m);
    }

    private async Task<string> CreateAthleteAsync(string firstName, string lastName)
    {
        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithFirstName(firstName)
            .WithLastName(lastName)
            .WithDateOfBirth(new DateOnly(1990, 1, 1))
            .Build();

        HttpResponseMessage response = await _setupHttpClient.PostAsJsonAsync(
            "/athletes", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        string slug = Slug.Create($"{firstName} {lastName}");
        _athleteSlugs.Add(slug);
        return slug;
    }

    private async Task<int> AddParticipantAsync(string athleteSlug)
    {
        AddParticipantCommand command = new AddParticipantCommandBuilder()
            .WithAthleteSlug(athleteSlug)
            .WithBodyWeight(82.0m)
            .WithAgeCategorySlug("open")
            .Build();

        HttpResponseMessage response = await _setupHttpClient.PostAsJsonAsync(
            $"/meets/{_meetId}/participants", command, CancellationToken.None);
        response.EnsureSuccessStatusCode();

        AddParticipantResponse? result = await response.Content
            .ReadFromJsonAsync<AddParticipantResponse>(CancellationToken.None);

        return result!.ParticipationId;
    }

    private async Task RecordFullTotalAsync(int participationId, decimal squat, decimal bench, decimal deadlift)
    {
        await RecordAttempt(participationId, Discipline.Squat, 1, squat, true);
        await RecordAttempt(participationId, Discipline.Bench, 1, bench, true);
        await RecordAttempt(participationId, Discipline.Deadlift, 1, deadlift, true);
    }

    private async Task RecordAttempt(
        int participationId,
        Discipline discipline,
        int round,
        decimal weight,
        bool good)
    {
        RecordAttemptCommand command = new RecordAttemptCommandBuilder()
            .WithWeight(weight)
            .WithGood(good)
            .Build();

        HttpResponseMessage response = await _setupHttpClient.PutAsJsonAsync(
            $"/meets/{_meetId}/participants/{participationId}/attempts/{(int)discipline}/{round}",
            command,
            CancellationToken.None);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }
}