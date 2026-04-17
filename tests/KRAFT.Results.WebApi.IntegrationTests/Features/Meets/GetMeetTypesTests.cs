using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(MeetsCollection))]
public sealed class GetMeetTypesTests
{
    private const string Path = "/meets/types";
    private const int ExpectedCount = 5;

    private readonly HttpClient _unauthorizedHttpClient;

    public GetMeetTypesTests(CollectionFixture fixture)
    {
        _unauthorizedHttpClient = fixture.Factory!.CreateClient();
    }

    [Fact]
    public async Task ReturnsOk()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(Path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsFiveItems()
    {
        // Arrange

        // Act
        IReadOnlyList<MeetTypeSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<MeetTypeSummary>>(Path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        response.Count.ShouldBe(ExpectedCount);
    }

    [Fact]
    public async Task ReturnsPowerliftingAsFirstItem()
    {
        // Arrange

        // Act
        IReadOnlyList<MeetTypeSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<MeetTypeSummary>>(Path, CancellationToken.None);

        // Assert
        MeetTypeSummary first = response.ShouldNotBeNull()[0];
        first.ShouldSatisfyAllConditions(
            () => first.Id.ShouldBe(1),
            () => first.Title.ShouldBe("Powerlifting"));
    }
}