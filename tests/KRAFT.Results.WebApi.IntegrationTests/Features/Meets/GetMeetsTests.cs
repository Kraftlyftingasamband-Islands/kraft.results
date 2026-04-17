using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(MeetsCollection))]
public sealed class GetMeetsTests
{
    private const string Path = "/meets";

    private readonly HttpClient _authorizedHttpClient;
    private readonly HttpClient _unauthorizedHttpClient;

    public GetMeetsTests(CollectionFixture fixture)
    {
        _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
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
    public async Task Deserializes()
    {
        // Arrange

        // Act
        IReadOnlyList<MeetSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<MeetSummary>>(Path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsMeets()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder().Build();
        await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);

        // Act
        IReadOnlyList<MeetSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<MeetSummary>>(Path, CancellationToken.None);

        // Assert
        response!.ShouldContain(x => x.Title == command.Title);
    }

    [Fact]
    public async Task OnlyReturnsMeetsWithSpecifiedYear()
    {
        // Arrange
        int year = 2023;
        string path = $"{Path}?year={year}";

        CreateMeetCommand firstCommand = new CreateMeetCommandBuilder()
            .WithStartDate(new DateOnly(year, 1, 1))
            .Build();
        await _authorizedHttpClient.PostAsJsonAsync(Path, firstCommand, CancellationToken.None);

        CreateMeetCommand secondCommand = new CreateMeetCommandBuilder()
            .WithStartDate(new DateOnly(2025, 1, 1))
            .Build();
        await _authorizedHttpClient.PostAsJsonAsync(Path, secondCommand, CancellationToken.None);

        // Act
        IReadOnlyList<MeetSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<MeetSummary>>(path, CancellationToken.None);

        // Assert
        response!.ShouldAllBe(x => x.StartDate.Year == year);
    }
}