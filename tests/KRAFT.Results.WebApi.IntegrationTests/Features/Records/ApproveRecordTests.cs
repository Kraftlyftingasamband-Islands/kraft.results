using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

public sealed class ApproveRecordTests(IntegrationTestFixture fixture)
{
    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsNoContent_WhenAttemptBeatsCurrentRecord()
    {
        // Arrange
        int attemptId = Constants.PendingRecords.ApproveAttemptId;

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsync(
            $"/meets/{Constants.TestMeetSlug}/pending-records/{attemptId}/approve",
            null,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenAttemptAlreadyHasRecord()
    {
        // Arrange — attempt 1 already has a record linked via AttemptId
        int attemptId = 1;

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsync(
            $"/meets/{Constants.TestMeetSlug}/pending-records/{attemptId}/approve",
            null,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Meets.AlreadyHasRecord");
    }

    [Fact]
    public async Task ReturnsNotFound_WhenAttemptDoesNotExist()
    {
        // Arrange
        int attemptId = 99999;

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsync(
            $"/meets/{Constants.TestMeetSlug}/pending-records/{attemptId}/approve",
            null,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Meets.AttemptNotFound");
    }

    [Fact]
    public async Task ReturnsNotFound_WhenAttemptIsInDifferentMeet()
    {
        // Arrange — attempt exists but not in this meet
        int attemptId = Constants.PendingRecords.RecordBreakingAttemptId;

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsync(
            $"/meets/non-existent-meet/pending-records/{attemptId}/approve",
            null,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        int attemptId = Constants.PendingRecords.RecordBreakingAttemptId;

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PutAsync(
            $"/meets/{Constants.TestMeetSlug}/pending-records/{attemptId}/approve",
            null,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}