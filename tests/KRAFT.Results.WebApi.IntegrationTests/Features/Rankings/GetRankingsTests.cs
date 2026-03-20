using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Rankings;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Rankings;

public sealed class GetRankingsTests(IntegrationTestFixture fixture)
{
    private const string Path = "/rankings";

    private readonly HttpClient _httpClient = fixture.Factory.CreateClient();

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
    public async Task Deserializes()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(Path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        response.Items.ShouldNotBeNull();
    }

    [Fact]
    public async Task DefaultsToTotalDiscipline()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(Path, CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
        response.Items[0].Result.ShouldBe(580.0m);
    }

    [Fact]
    public async Task FiltersbyYear()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>($"{Path}?year=2025", CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ReturnsEmpty_WhenYearHasNoData()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>($"{Path}?year=1900", CancellationToken.None);

        // Assert
        response!.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task FiltersByGender()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>($"{Path}?gender=m", CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ReturnsEmpty_WhenGenderHasNoData()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>($"{Path}?gender=f", CancellationToken.None);

        // Assert
        response!.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task FiltersByEquipmentType()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>($"{Path}?equipmentType=classic", CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ReturnsEmpty_WhenEquipmentTypeHasNoData()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>($"{Path}?equipmentType=equipped", CancellationToken.None);

        // Assert
        response!.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task FiltersByDiscipline_Squat()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>($"{Path}?discipline=squat", CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
        response.Items[0].Result.ShouldBe(200.0m);
    }

    [Fact]
    public async Task FiltersByDiscipline_Bench()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>($"{Path}?discipline=bench", CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
        response.Items[0].Result.ShouldBe(130.0m);
    }

    [Fact]
    public async Task FiltersByDiscipline_Deadlift()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>($"{Path}?discipline=deadlift", CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
        response.Items[0].Result.ShouldBe(250.0m);
    }

    [Fact]
    public async Task ExcludesDisqualified()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>($"{Path}?year=2025", CancellationToken.None);

        // Assert
        response!.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Paginates()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>($"{Path}?page=1&pageSize=1", CancellationToken.None);

        // Assert
        response!.Items.Count.ShouldBe(1);
        response.Page.ShouldBe(1);
        response.PageSize.ShouldBe(1);
    }

    [Fact]
    public async Task OrdersDescendingByResult()
    {
        // Arrange

        // Act
        PagedResponse<RankingEntry>? response = await _httpClient.GetFromJsonAsync<PagedResponse<RankingEntry>>(Path, CancellationToken.None);

        // Assert
        response!.Items.ShouldNotBeEmpty();
        response.Items[0].Rank.ShouldBe(1);
    }
}