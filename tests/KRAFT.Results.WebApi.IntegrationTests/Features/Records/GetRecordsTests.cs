using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Records;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

[Collection(nameof(RecordsCollection))]
public sealed class GetRecordsTests(CollectionFixture fixture)
{
    private const string Path = "/records";

    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();

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

        int maleNonEmptyCount = maleGroups
            .SelectMany(g => g.Records)
            .Count(r => r.Athlete is not null);
        int femaleNonEmptyCount = femaleGroups
            .SelectMany(g => g.Records)
            .Count(r => r.Athlete is not null);

        maleNonEmptyCount.ShouldBeGreaterThan(femaleNonEmptyCount);
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

        int openNonEmptyCount = openGroups
            .SelectMany(g => g.Records)
            .Count(r => r.Athlete is not null);
        int juniorNonEmptyCount = juniorGroups
            .SelectMany(g => g.Records)
            .Count(r => r.Athlete is not null);

        openNonEmptyCount.ShouldBeGreaterThan(juniorNonEmptyCount);
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

        int classicNonEmptyCount = classicGroups
            .SelectMany(g => g.Records)
            .Count(r => r.Athlete is not null);
        classicNonEmptyCount.ShouldBe(1);
    }

    [Fact]
    public async Task ReturnsAllRecordCategories_EvenWithNoRecords()
    {
        // Arrange — female junior equipped has no records but has active weight categories

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=f&ageCategory=junior&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        groups.Count.ShouldBe(6);
        groups.SelectMany(g => g.Records).ShouldAllBe(r => r.Athlete == null);
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

    [Fact]
    public async Task ExcludesWeightCategoriesNotInCurrentEra()
    {
        // Arrange — weight category 5 (105kg) has no EraWeightCategory row

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        List<RecordEntry> allRecords = groups.SelectMany(g => g.Records).ToList();
        allRecords.ShouldNotContain(r => r.WeightCategory == "105");
    }

    [Fact]
    public async Task ExcludesJuniorsOnlyWeightCategories_ForNonJuniorAgeCategory()
    {
        // Arrange — weight category 4 (74kg) is JuniorsOnly

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        List<RecordEntry> allRecords = groups.SelectMany(g => g.Records).ToList();
        allRecords.ShouldNotContain(r => r.WeightCategory == "74");
    }

    [Fact]
    public async Task ReturnsHighestWeight_WhenMultipleRecordsExist()
    {
        // Arrange — two squat records for (RecordCategoryId=1, WeightCategoryId=1): 200.0 and 190.0

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        RecordGroup squatGroup = groups.First(g => g.Category == "Hn\u00e9beygja");
        RecordEntry squatRecord83 = squatGroup.Records.First(r => r.WeightCategory == "83");
        squatRecord83.Weight.ShouldBe(200.0m);
    }

    [Fact]
    public async Task SortsWeightCategoriesNumerically()
    {
        // Arrange — open equipped male has weight categories 83 and 93

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        RecordGroup squatGroup = groups.First(g => g.Category == "Hn\u00e9beygja");
        List<string> weightCategories = squatGroup.Records.Select(r => r.WeightCategory).ToList();
        weightCategories.Count.ShouldBeGreaterThanOrEqualTo(2);
        weightCategories.ShouldBe(weightCategories.OrderBy(w => decimal.Parse(w, System.Globalization.CultureInfo.InvariantCulture)).ToList());
    }

    [Fact]
    public async Task ReturnsHighestWeight_WhenIsCurrentFlagIsCorrupt()
    {
        // Arrange — bench record for 93kg: 150.0 (IsCurrent=0) vs 140.0 (IsCurrent=1)
        // The handler should return 150.0 (highest weight) regardless of IsCurrent flag

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        RecordGroup benchGroup = groups.First(g => g.Category == "Bekkpressa");
        RecordEntry benchRecord93 = benchGroup.Records.First(r => r.WeightCategory == "93");
        benchRecord93.Weight.ShouldBe(150.0m);
    }

    [Fact]
    public async Task ReturnsIsStandardFalse_WhenRecordHasAthleteEvenIfDbFlagIsTrue()
    {
        // Arrange — BenchSingle 83kg has IsStandard=1 in DB but is linked to a real athlete via AttemptId

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        RecordGroup benchSingleGroup = groups.First(g => g.Category == "Bekkpressa (stök grein)");
        RecordEntry benchSingleRecord83 = benchSingleGroup.Records.First(r => r.WeightCategory == "83");
        benchSingleRecord83.Athlete.ShouldNotBeNull();
        benchSingleRecord83.IsStandard.ShouldBeFalse();
    }

    [Fact]
    public async Task ReturnsEmptyEntries_ForWeightCategoriesWithNoRecords()
    {
        // Arrange — 93kg has no deadlift record (equipped, open, male)

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();
        RecordGroup deadliftGroup = groups.First(g => g.Category == "R\u00e9ttst\u00f6\u00f0ulyfta");
        RecordEntry deadliftRecord93 = deadliftGroup.Records.First(r => r.WeightCategory == "93");
        deadliftRecord93.Athlete.ShouldBeNull();
        deadliftRecord93.Weight.ShouldBe(0m);
    }

    [Fact]
    public async Task ReturnsAllActiveWeightCategories_EvenWithNoRecords()
    {
        // Arrange — open equipped male: active weight categories are 83kg and 93kg
        // 93kg only has standard squat record + corrupt bench records, other categories have no records

        // Act
        List<RecordGroup>? groups = await _httpClient.GetFromJsonAsync<List<RecordGroup>>(
            $"{Path}?gender=m&ageCategory=open&equipmentType=equipped",
            CancellationToken.None);

        // Assert
        groups.ShouldNotBeNull();

        foreach (RecordGroup group in groups)
        {
            List<string> weightCategories = group.Records.Select(r => r.WeightCategory).ToList();
            weightCategories.ShouldContain("83");
            weightCategories.ShouldContain("93");
        }
    }
}