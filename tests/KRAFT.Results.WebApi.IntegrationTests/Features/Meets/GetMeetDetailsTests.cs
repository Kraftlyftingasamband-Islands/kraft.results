using System.Net.Http.Json;

using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(MeetsCollection))]
public sealed class GetMeetDetailsTests(CollectionFixture fixture) : IAsyncLifetime
{
    private const string BasePath = "/meets";

    private readonly HttpClient _httpClient = fixture.Factory!.CreateClient();
    private readonly HttpClient _authorizedClient = fixture.CreateAuthorizedHttpClient();

    private string _meetSlug = string.Empty;
    private int _meetId;

    async ValueTask IAsyncLifetime.InitializeAsync()
    {
        CreateMeetCommand meetCommand = new CreateMeetCommandBuilder().Build();

        HttpResponseMessage createMeetResponse = await _authorizedClient.PostAsJsonAsync(
            "/meets",
            meetCommand,
            CancellationToken.None);

        createMeetResponse.EnsureSuccessStatusCode();

        _meetSlug = createMeetResponse.Headers.Location!.ToString().TrimStart('/');

        MeetDetails? meetDetails = await _authorizedClient.GetFromJsonAsync<MeetDetails>(
            $"/meets/{_meetSlug}",
            CancellationToken.None);

        _meetId = meetDetails!.MeetId;
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (_meetSlug.Length > 0)
        {
            await _authorizedClient.DeleteAsync($"/meets/{_meetSlug}", CancellationToken.None);
        }

        _httpClient.Dispose();
        _authorizedClient.Dispose();
    }

    [Fact]
    public async Task WhenMeetHasPhotosWithValidFilenames_ReturnsPhotoCount()
    {
        // Arrange — seed three photos: two with valid filenames, one with null filename
        await fixture.ExecuteSqlAsync(
            $"""
            INSERT INTO dbo.Photos (MeetId, Photographer, Date, ImageFilname, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy)
            VALUES
                ({_meetId}, NULL, GETUTCDATE(), 'photo-a.jpg', '2024-01-01', 'test-setup', '2024-01-01', 'test-setup'),
                ({_meetId}, NULL, GETUTCDATE(), 'photo-b.jpg', '2024-01-02', 'test-setup', '2024-01-02', 'test-setup'),
                ({_meetId}, NULL, GETUTCDATE(), NULL,          '2024-01-03', 'test-setup', '2024-01-03', 'test-setup')
            """);

        // Act
        MeetDetails? result = await _httpClient.GetFromJsonAsync<MeetDetails>(
            $"{BasePath}/{_meetSlug}",
            CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.PhotoCount.ShouldBe(2);
    }

    [Fact]
    public async Task WhenFilenameIsEmpty_ExcludesFromPhotoCount()
    {
        // Arrange — seed two photos: one with valid filename, one with empty string
        await fixture.ExecuteSqlAsync(
            $"""
            INSERT INTO dbo.Photos (MeetId, Photographer, Date, ImageFilname, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy)
            VALUES
                ({_meetId}, NULL, GETUTCDATE(), 'valid.jpg', '2024-02-01', 'test-setup', '2024-02-01', 'test-setup'),
                ({_meetId}, NULL, GETUTCDATE(), '',          '2024-02-02', 'test-setup', '2024-02-02', 'test-setup')
            """);

        // Act
        MeetDetails? result = await _httpClient.GetFromJsonAsync<MeetDetails>(
            $"{BasePath}/{_meetSlug}",
            CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.PhotoCount.ShouldBe(1);
    }
}
