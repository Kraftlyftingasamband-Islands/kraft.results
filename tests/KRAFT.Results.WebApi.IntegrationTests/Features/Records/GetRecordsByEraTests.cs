using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Records;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

public sealed class GetRecordsByEraTests(IntegrationTestFixture fixture)
{
    private const string Path = "/records";

    private readonly HttpClient _httpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsCurrentEraRecords_WhenNoEraSpecified()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.ShouldNotBeEmpty();
        groups.ShouldContain(g => g.Category == "Hn\u00e9beygja");
    }

    [Fact]
    public async Task ReturnsHistoricalEraRecords_WhenEraSlugSpecified()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped&era=historical-era",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.ShouldNotBeEmpty();
        RecordGroup squatGroup = groups.First(g => g.Category == "Hn\u00e9beygja");
        squatGroup.Records.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task ReturnsHistoricalEraRecords_WithCorrectWeightCategories()
    {
        // Arrange — historical era has 83kg and 105kg weight categories

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped&era=historical-era",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        List<RecordEntry> allRecords = groups.SelectMany(g => g.Records).ToList();
        allRecords.ShouldContain(r => r.WeightCategory == "83");
        allRecords.ShouldContain(r => r.WeightCategory == "105");
    }

    [Fact]
    public async Task DoesNotReturnCurrentEraRecords_ForHistoricalEra()
    {
        // Arrange — current era has 93kg records, historical does not

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped&era=historical-era",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        List<RecordEntry> allRecords = groups.SelectMany(g => g.Records).ToList();
        allRecords.ShouldNotContain(r => r.WeightCategory == "93");
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenEraSlugIsUnknown()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped&era=nonexistent-era",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ReturnsCurrentEraRecords_WhenExplicitCurrentEraSlugSpecified()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped&era=current-era",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.ShouldNotBeEmpty();
        groups.ShouldContain(g => g.Category == "Hn\u00e9beygja");
    }

    [Fact]
    public async Task HistoricalEraExcludes105kg_FromCurrentEra()
    {
        // Arrange — 105kg is NOT in current era's EraWeightCategories

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        List<RecordEntry> allRecords = groups.SelectMany(g => g.Records).ToList();
        allRecords.ShouldNotContain(r => r.WeightCategory == "105");
    }
}