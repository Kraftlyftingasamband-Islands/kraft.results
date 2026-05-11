using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(MeetsCollection))]
public sealed class GetMeetsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string Path = "/meets";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();
    private string _meetSlug = string.Empty;
    private string _meetTitle = string.Empty;

    public async ValueTask InitializeAsync()
    {
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(new DateOnly(2099, 1, 1))
            .Build();

        HttpResponseMessage createResponse = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();

        _meetSlug = createResponse.Headers.Location!.ToString().TrimStart('/');
        _meetTitle = command.Title;
    }

    public async ValueTask DisposeAsync()
    {
        if (!string.IsNullOrEmpty(_meetSlug))
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{_meetSlug}", CancellationToken.None);
        }

        _authorizedHttpClient.Dispose();
        _unauthorizedHttpClient.Dispose();
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

        // Act
        IReadOnlyList<MeetSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<MeetSummary>>(Path, CancellationToken.None);

        // Assert
        response!.ShouldContain(x => x.Title == _meetTitle);
    }

    [Fact]
    public async Task ReturnsDisciplineAndIsClassicAndParticipantCount()
    {
        // Arrange
        CreateMeetCommand command = new CreateMeetCommandBuilder()
            .WithStartDate(new DateOnly(2099, 2, 1))
            .WithMeetTypeId(1)
            .WithIsRaw(true)
            .Build();

        HttpResponseMessage createResponse = await _authorizedHttpClient.PostAsJsonAsync(Path, command, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();
        string slug = createResponse.Headers.Location!.ToString().TrimStart('/');

        try
        {
            // Act
            IReadOnlyList<MeetSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<MeetSummary>>(Path, CancellationToken.None);

            // Assert
            IReadOnlyList<MeetSummary> meets = response.ShouldNotBeNull();
            meets.ShouldContain(x => x.Slug == slug);
            MeetSummary meet = meets.First(x => x.Slug == slug);
            meet.Discipline.ShouldBe(KRAFT.Results.Contracts.Constants.Powerlifting);
            meet.IsClassic.ShouldBeTrue();
            meet.ParticipantCount.ShouldBe(0);
        }
        finally
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{slug}", CancellationToken.None);
        }
    }

    [Fact]
    public async Task OnlyReturnsMeetsWithSpecifiedYear()
    {
        // Arrange
        int year = 2099;
        string path = $"{Path}?year={year}";

        CreateMeetCommand secondCommand = new CreateMeetCommandBuilder()
            .WithStartDate(new DateOnly(2025, 1, 1))
            .Build();
        HttpResponseMessage secondCreateResponse = await _authorizedHttpClient.PostAsJsonAsync(Path, secondCommand, CancellationToken.None);
        secondCreateResponse.EnsureSuccessStatusCode();
        string secondMeetSlug = secondCreateResponse.Headers.Location!.ToString().TrimStart('/');

        try
        {
            // Act
            IReadOnlyList<MeetSummary>? response = await _unauthorizedHttpClient.GetFromJsonAsync<IReadOnlyList<MeetSummary>>(path, CancellationToken.None);

            // Assert
            response!.ShouldNotBeEmpty();
            response.ShouldAllBe(x => x.StartDate.Year == year);
        }
        finally
        {
            await _authorizedHttpClient.DeleteAsync($"/meets/{secondMeetSlug}", CancellationToken.None);
        }
    }
}