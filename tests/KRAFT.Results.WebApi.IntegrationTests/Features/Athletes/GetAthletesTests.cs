using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.WebApi.IntegrationTests.Builders;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Athletes;

public sealed class GetAthletesTests : IClassFixture<IntegrationTestFixture>
{
    private const string Path = "/athletes";

    private readonly HttpClient _authorizedHttpClient;
    private readonly HttpClient _unauthorizedHttpClient;

    public GetAthletesTests(IntegrationTestFixture fixture)
    {
        _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
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
        IReadOnlyList<AthleteSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<AthleteSummary>>(Path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsTestAthlete()
    {
        // Arrange

        // Act
        IReadOnlyList<AthleteSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<AthleteSummary>>(Path, CancellationToken.None);

        // Assert
        response!.ShouldContain(x => x.Slug == Constants.TestAthleteSlug);
    }

    [Fact]
    public async Task ReturnsTestAthletesOrderedByFirstName()
    {
        // Arrange
        CreateAthleteCommand command = new CreateAthleteCommandBuilder()
            .WithFirstName("A")
            .WithLastName("A")
            .Build();
        await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Act
        IReadOnlyList<AthleteSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<AthleteSummary>>(Path, CancellationToken.None);

        // Assert
        response![0].Name.ShouldBe("A A");
    }
}