using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Records;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

[Collection(nameof(RecordsCollection))]
public sealed class GetRecordHistoryTests(CollectionFixture fixture)
{
    private const string RecordsPath = "/records";

    private readonly HttpClient _httpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsOk_WithValidRecordId()
    {
        // Arrange
        int recordId = await GetFirstRecordIdAsync();

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{RecordsPath}/{recordId}/history",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsNotFound_WithInvalidRecordId()
    {
        // Arrange
        int invalidId = 999999;

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{RecordsPath}/{invalidId}/history",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsEntriesOrderedByDate()
    {
        // Arrange
        int recordId = await GetFirstRecordIdAsync();

        // Act
        RecordHistoryResponse? history = await _httpClient.GetFromJsonAsync<RecordHistoryResponse>(
            $"{RecordsPath}/{recordId}/history",
            CancellationToken.None);

        // Assert
        history.ShouldNotBeNull();
        history.Entries.ShouldNotBeEmpty();

        List<DateOnly> dates = history.Entries
            .Select(e => e.Date)
            .ToList();

        dates.ShouldBe(dates.OrderBy(d => d).ToList());
    }

    [Fact]
    public async Task CurrentRecordIsMarked()
    {
        // Arrange
        int recordId = await GetFirstRecordIdAsync();

        // Act
        RecordHistoryResponse? history = await _httpClient.GetFromJsonAsync<RecordHistoryResponse>(
            $"{RecordsPath}/{recordId}/history",
            CancellationToken.None);

        // Assert
        history.ShouldNotBeNull();
        history.Entries.Count(e => e.IsCurrent).ShouldBe(1);
    }

    [Fact]
    public async Task ResponseIncludesMetadata()
    {
        // Arrange
        int recordId = await GetFirstRecordIdAsync();

        // Act
        RecordHistoryResponse? history = await _httpClient.GetFromJsonAsync<RecordHistoryResponse>(
            $"{RecordsPath}/{recordId}/history",
            CancellationToken.None);

        // Assert
        history.ShouldNotBeNull();
        history.Category.ShouldNotBeNullOrWhiteSpace();
        history.WeightCategory.ShouldNotBeNullOrWhiteSpace();
        history.AgeCategory.ShouldNotBeNullOrWhiteSpace();
        history.Gender.ShouldNotBeNullOrWhiteSpace();
        history.EquipmentType.ShouldNotBeNullOrWhiteSpace();
    }

    private async Task<int> GetFirstRecordIdAsync()
    {
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{RecordsPath}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        groups.ShouldNotBeNull();
        groups.ShouldNotBeEmpty();

        RecordEntry firstRecord = groups
            .SelectMany(g => g.Records)
            .First();

        return firstRecord.Id;
    }
}