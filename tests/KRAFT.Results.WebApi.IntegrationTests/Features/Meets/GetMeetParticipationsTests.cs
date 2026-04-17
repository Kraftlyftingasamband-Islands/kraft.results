using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(MeetsCollection))]
public sealed class GetMeetParticipationsTests
{
    private const string Path = $"/meets/{Constants.OrderingMeet.Slug}/participations";

    private readonly HttpClient _httpClient;

    public GetMeetParticipationsTests(CollectionFixture fixture)
    {
        _httpClient = fixture.Factory.CreateClient();
    }

    [Fact]
    public async Task ReturnsOk()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(Path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PlacedParticipantsAppearBeforeDisqualified()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(Path, CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        participations.Count.ShouldBe(4);

        List<MeetParticipation> placed = participations.Where(p => p.Rank > 0).ToList();
        List<MeetParticipation> unplaced = participations.Where(p => p.Rank <= 0).ToList();

        int lastPlacedIndex = participations.IndexOf(placed[^1]);
        int firstUnplacedIndex = participations.IndexOf(unplaced[0]);

        lastPlacedIndex.ShouldBeLessThan(firstUnplacedIndex);
    }

    [Fact]
    public async Task DisqualifiedParticipantAppearsLast()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(Path, CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation last = participations[^1];
        last.Disqualified.ShouldBeTrue();
        last.Athlete.ShouldBe("Anna Test");
    }

    [Fact]
    public async Task ParticipantsWithSameRankAreSortedAlphabetically()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(Path, CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        List<MeetParticipation> tiedAtFirst = participations.Where(p => p.Rank == 1).ToList();
        tiedAtFirst.Count.ShouldBe(2);
        tiedAtFirst[0].Athlete.ShouldBe("Charlie Test");
        tiedAtFirst[1].Athlete.ShouldBe("Delta Test");
    }

    [Fact]
    public async Task ParticipationsAreOrderedByRankAscending()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(Path, CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        List<MeetParticipation> placed = participations.Where(p => p.Rank > 0).ToList();
        List<int> ranks = placed.Select(p => p.Rank).ToList();
        ranks.ShouldBe(ranks.OrderBy(r => r).ToList());
    }

    [Fact]
    public async Task DisqualifiedFieldIsTrueForDisqualifiedParticipant()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(Path, CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation disqualified = participations.Single(p => p.Athlete == "Anna Test");
        disqualified.Disqualified.ShouldBeTrue();
    }

    [Fact]
    public async Task AgeCategoryIsTranslatedToIcelandic()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(Path, CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation participation = participations.First(p => p.Athlete == "Charlie Test");
        participation.AgeCategory.ShouldBe("Opinn flokkur");
        participation.AgeCategorySlug.ShouldBe("open");
    }

    [Fact]
    public async Task DisqualifiedFieldIsFalseForPlacedParticipant()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(Path, CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation placed = participations.First(p => p.Athlete == "Bob Test");
        placed.Disqualified.ShouldBeFalse();
    }

    [Fact]
    public async Task NonDisqualifiedParticipant_IpfPointsAreCalculatedFromTotal()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(Path, CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation participation = participations.First(p => p.Athlete == "Delta Test");
        participation.IpfPoints.ShouldBeGreaterThan(78m);
        participation.IpfPoints.ShouldBeLessThan(83m);
    }

    [Fact]
    public async Task DisqualifiedParticipant_IpfPointsAreZero()
    {
        // Arrange

        // Act
        List<MeetParticipation>? participations = await _httpClient.GetFromJsonAsync<List<MeetParticipation>>(Path, CancellationToken.None);

        // Assert
        participations.ShouldNotBeNull();
        MeetParticipation participation = participations.Single(p => p.Athlete == "Anna Test");
        participation.IpfPoints.ShouldBe(0m);
    }
}