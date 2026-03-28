using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Records;

public sealed class ApproveRecordTests(IntegrationTestFixture fixture)
{
    private const string BasePath = "/records";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory.CreateClient();

    [Fact]
    public async Task ReturnsNoContent_WhenRecordIsPending()
    {
        // Arrange
        int recordId = Constants.PendingRecordForApprovalId;

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsync(
            $"{BasePath}/{recordId}/approve",
            null,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task ReturnsBadRequest_WhenRecordIsNotPending()
    {
        // Arrange — record 1 is already approved via seed backfill
        int recordId = 1;

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsync(
            $"{BasePath}/{recordId}/approve",
            null,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Records.NotPending");
    }

    [Fact]
    public async Task ReturnsNotFound_WhenRecordDoesNotExist()
    {
        // Arrange
        int recordId = 99999;

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.PutAsync(
            $"{BasePath}/{recordId}/approve",
            null,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        ErrorResponse? error = await response.Content.ReadFromJsonAsync<ErrorResponse>(CancellationToken.None);
        error.ShouldNotBeNull();
        error.Code.ShouldBe("Records.NotFound");
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenNotAuthenticated()
    {
        // Arrange
        int recordId = Constants.PendingRecordForApprovalId;

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.PutAsync(
            $"{BasePath}/{recordId}/approve",
            null,
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}