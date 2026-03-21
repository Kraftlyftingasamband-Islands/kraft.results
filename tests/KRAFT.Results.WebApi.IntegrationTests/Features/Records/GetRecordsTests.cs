using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Records;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

public sealed class GetRecordsTests(IntegrationTestFixture fixture)
{
    private const string Path = "/records";

    private readonly HttpClient _httpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsOk_WithValidFilters()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReturnsGroupedRecords()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.Count.ShouldBeGreaterThanOrEqualTo(3);
        groups.ShouldContain(g => g.Category == "Hn\u00e9beygja");
        groups.ShouldContain(g => g.Category == "Bekkpressa");
        groups.ShouldContain(g => g.Category == "R\u00e9ttst\u00f6\u00f0ulyfta");
        groups.ShouldContain(g => g.Category == "Samtala");
    }

    [Fact]
    public async Task FiltersByGender()
    {
        // Arrange

        // Act
        List<RecordGroup>? maleGroups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        List<RecordGroup>? femaleGroups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=f&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        maleGroups.ShouldNotBeNull();
        maleGroups.ShouldNotBeEmpty();

        femaleGroups.ShouldNotBeNull();
        femaleGroups.ShouldNotBeEmpty();

        int maleRecordCount = maleGroups.Sum(g => g.Records.Count);
        int femaleRecordCount = femaleGroups.Sum(g => g.Records.Count);

        maleRecordCount.ShouldBeGreaterThan(femaleRecordCount);
    }

    [Fact]
    public async Task FiltersByAgeCategory()
    {
        // Arrange

        // Act
        List<RecordGroup>? openGroups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        List<RecordGroup>? juniorGroups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=junior&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        openGroups.ShouldNotBeNull();
        openGroups.ShouldNotBeEmpty();

        juniorGroups.ShouldNotBeNull();
        juniorGroups.ShouldNotBeEmpty();

        int openCount = openGroups.Sum(g => g.Records.Count);
        int juniorCount = juniorGroups.Sum(g => g.Records.Count);

        openCount.ShouldBeGreaterThan(juniorCount);
    }

    [Fact]
    public async Task FiltersByEquipmentType_Classic()
    {
        // Arrange

        // Act
        List<RecordGroup>? classicGroups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=classic",
            CancellationToken.None);

        // Assert
        classicGroups.ShouldNotBeNull();
        classicGroups.ShouldNotBeEmpty();

        int classicCount = classicGroups.Sum(g => g.Records.Count);
        classicCount.ShouldBe(1);
    }

    [Fact]
    public async Task ReturnsEmpty_WhenNoMatchingRecords()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=f&ageCategory=junior&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.ShouldBeEmpty();
    }

    [Fact]
    public async Task IncludesStandardRecords()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        List<RecordEntry> allRecords = groups.SelectMany(g => g.Records).ToList();
        allRecords.ShouldContain(r => r.IsStandard);
    }

    [Fact]
    public async Task ExcludesTotalWilksAndTotalIpfPoints()
    {
        // Arrange

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.ShouldNotContain(g => g.Category == string.Empty);
    }
}