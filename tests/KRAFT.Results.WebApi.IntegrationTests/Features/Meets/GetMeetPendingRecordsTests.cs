using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Records;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

public sealed class GetMeetPendingRecordsTests(IntegrationTestFixture fixture)
{
    private const string BasePath = "/meets";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsOk_WithPendingRecords_WhenMeetHasRecordBreakingAttempts()
    {
        // Arrange
        string slug = Constants.TestMeetSlug;

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.GetAsync(
            $"{BasePath}/{slug}/pending-records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<PendingRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<PendingRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();
        records.Count.ShouldBeGreaterThanOrEqualTo(1);

        records.ShouldContain(r => r.AttemptId == Constants.PendingRecords.RecordBreakingAttemptId);

        PendingRecordEntry recordBreaker = records.First(
            r => r.AttemptId == Constants.PendingRecords.RecordBreakingAttemptId);
        recordBreaker.Weight.ShouldBe(210.0m);
    }

    [Fact]
    public async Task DoesNotInclude_AttemptsAlreadyLinkedToRecords()
    {
        // Arrange — attempts 1, 2, 3 all have Record rows linked via AttemptId
        string slug = Constants.TestMeetSlug;

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.GetAsync(
            $"{BasePath}/{slug}/pending-records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<PendingRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<PendingRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();
        records.ShouldNotContain(r => r.AttemptId == 1);
        records.ShouldNotContain(r => r.AttemptId == 2);
        records.ShouldNotContain(r => r.AttemptId == 3);
    }

    [Fact]
    public async Task DoesNotInclude_AttemptsBelowCurrentRecord()
    {
        // Arrange — attempt 5 is a 190kg squat, current equipped record is 200kg
        string slug = Constants.TestMeetSlug;

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.GetAsync(
            $"{BasePath}/{slug}/pending-records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        List<PendingRecordEntry>? records = await response.Content
            .ReadFromJsonAsync<List<PendingRecordEntry>>(CancellationToken.None);
        records.ShouldNotBeNull();
        records.ShouldNotContain(r => r.AttemptId == Constants.PendingRecords.NonRecordBreakingAttemptId);
    }

    [Fact]
    public async Task ReturnsNotFound_WhenMeetDoesNotExist()
    {
        // Arrange & Act
        HttpResponseMessage response = await _authorizedHttpClient.GetAsync(
            $"{BasePath}/non-existent-meet/pending-records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        string slug = Constants.TestMeetSlug;

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(
            $"{BasePath}/{slug}/pending-records",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}