using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Meets;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

public sealed class GetMeetRecordsTests(IntegrationTestFixture fixture)
{
    private const string BasePath = "/meets";

    private readonly HttpClient _httpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsOk_WithRecords_WhenMeetHasApprovedRecords()
    {
        // Arrange
        string slug = Constants.TestMeetSlug;

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{slug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();
        records.Count.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task DoesNotInclude_StandardRecords()
    {
        // Arrange
        string slug = Constants.TestMeetSlug;

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{slug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();

        // Standard record is for 93kg squat (220kg) — should not appear
        records.ShouldNotContain(r => r.WeightCategory == "93" && r.Discipline == "Hnébeygja" && r.Weight == 220.0m);
    }

    [Fact]
    public async Task DoesNotInclude_TotalWilksRecords()
    {
        // Arrange
        string slug = Constants.TestMeetSlug;

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{slug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();
        records.ShouldNotContain(r => r.Weight == 400.0m);
    }

    [Fact]
    public async Task DoesNotInclude_TotalIpfPointsRecords()
    {
        // Arrange
        string slug = Constants.TestMeetSlug;

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{slug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();
        records.ShouldNotContain(r => r.Weight == 85.5m);
    }

    [Fact]
    public async Task IncludesCorrectFields_ForApprovedRecord()
    {
        // Arrange — seed has squat record: equipped, open, 83kg, 200.0kg, AttemptId=1 (Testie McTestFace)
        string slug = Constants.TestMeetSlug;

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{slug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();

        MeetRecordEntry squatRecord = records.First(r => r.Weight == 200.0m && r.Discipline == "Hnébeygja");
        squatRecord.AthleteName.ShouldBe("Testie McTestFace");
        squatRecord.AthleteSlug.ShouldBe("testie-mctestface");
        squatRecord.WeightCategory.ShouldBe("83");
        squatRecord.AgeCategory.ShouldBe("Open");
        squatRecord.IsClassic.ShouldBeFalse();
    }

    [Fact]
    public async Task IncludesClassicRecords_WithIsClassicTrue()
    {
        // Arrange — seed has classic squat record: raw, open, 83kg, 195.0kg
        string slug = Constants.TestMeetSlug;

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{slug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<MeetRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<MeetRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();

        MeetRecordEntry classicRecord = records.First(r => r.Weight == 195.0m);
        classicRecord.IsClassic.ShouldBeTrue();
    }

    [Fact]
    public async Task ReturnsNotFound_WhenMeetDoesNotExist()
    {
        // Arrange & Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/non-existent-meet/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task IsPublic_NoAuthRequired()
    {
        // Arrange — using unauthenticated client
        string slug = Constants.TestMeetSlug;

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{slug}/records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}