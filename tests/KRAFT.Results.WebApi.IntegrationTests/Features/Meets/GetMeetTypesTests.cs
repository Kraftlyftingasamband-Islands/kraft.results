using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Meets;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

public sealed class GetMeetTypesTests
{
    private const string Path = "/meets/types";

    private readonly HttpClient _unauthorizedHttpClient;

    public GetMeetTypesTests(IntegrationTestFixture fixture)
    {
        _unauthorizedHttpClient = fixture.Factory.CreateClient();
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
    public async Task Deserializes()
    {
        // Arrange

        // Act
        IReadOnlyList<MeetTypeSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<MeetTypeSummary>>(Path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsTestMeetType()
    {
        // Arrange

        // Act
        IReadOnlyList<MeetTypeSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<MeetTypeSummary>>(Path, CancellationToken.None);

        // Assert
        response!.ShouldContain(x => x.Title == Constants.TestMeetType);
    }
}