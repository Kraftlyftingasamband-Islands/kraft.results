using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Meets;

[Collection(nameof(MeetsCollection))]
public sealed class GetMeetPhotosTests(CollectionFixture fixture) : IAsyncLifetime
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
    public async Task WhenMeetHasNoPhotos_ReturnsOkWithEmptyPhotos()
    {
        // Arrange & Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{_meetSlug}/photos",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        MeetPhotos? result = await response.Content.ReadFromJsonAsync<MeetPhotos>(CancellationToken.None);
        result.ShouldNotBeNull();
        result.Photos.ShouldBeEmpty();
    }

    [Fact]
    public async Task WhenMeetDoesNotExist_ReturnsNotFound()
    {
        // Arrange & Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/non-existent-meet/photos",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WhenMeetHasPhotos_ReturnsOkWithPhotos()
    {
        // Arrange — seed two photos for the meet
        await fixture.ExecuteSqlAsync(
            $"""
            INSERT INTO dbo.Photos (MeetId, Photographer, Date, ImageFilname, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy)
            VALUES
                ({_meetId}, 'Test Photographer', GETUTCDATE(), 'photo1.jpg', '2024-01-01', 'test-setup', '2024-01-01', 'test-setup'),
                ({_meetId}, NULL,                 GETUTCDATE(), 'photo2.jpg', '2024-01-02', 'test-setup', '2024-01-02', 'test-setup')
            """);

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{_meetSlug}/photos",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        MeetPhotos? result = await response.Content.ReadFromJsonAsync<MeetPhotos>(CancellationToken.None);
        result.ShouldNotBeNull();
        result.Photos.Count.ShouldBe(2);
    }

    [Fact]
    public async Task WhenFilenameIsNullOrEmpty_ExcludesPhoto()
    {
        // Arrange — seed one valid photo and one with null filename
        await fixture.ExecuteSqlAsync(
            $"""
            INSERT INTO dbo.Photos (MeetId, Photographer, Date, ImageFilname, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy)
            VALUES
                ({_meetId}, 'Photographer A', GETUTCDATE(), 'valid.jpg', '2024-02-01', 'test-setup', '2024-02-01', 'test-setup'),
                ({_meetId}, 'Photographer B', GETUTCDATE(), NULL,        '2024-02-02', 'test-setup', '2024-02-02', 'test-setup'),
                ({_meetId}, 'Photographer C', GETUTCDATE(), '',          '2024-02-03', 'test-setup', '2024-02-03', 'test-setup')
            """);

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{_meetSlug}/photos",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        MeetPhotos? result = await response.Content.ReadFromJsonAsync<MeetPhotos>(CancellationToken.None);
        result.ShouldNotBeNull();
        result.Photos.ShouldContain(p => p.ImageFilename == "valid.jpg");
        result.Photos.ShouldNotContain(p => p.ImageFilename == null || p.ImageFilename == string.Empty);
    }

    [Fact]
    public async Task WhenPhotographerIsPresent_ReturnsName_WhenAbsent_ReturnsNull()
    {
        // Arrange
        await fixture.ExecuteSqlAsync(
            $"""
            INSERT INTO dbo.Photos (MeetId, Photographer, Date, ImageFilname, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy)
            VALUES
                ({_meetId}, 'Known Photographer', GETUTCDATE(), 'with-photographer.jpg', '2024-03-01', 'test-setup', '2024-03-01', 'test-setup'),
                ({_meetId}, NULL,                 GETUTCDATE(), 'no-photographer.jpg',   '2024-03-02', 'test-setup', '2024-03-02', 'test-setup')
            """);

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{_meetSlug}/photos",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        MeetPhotos? result = await response.Content.ReadFromJsonAsync<MeetPhotos>(CancellationToken.None);
        result.ShouldNotBeNull();
        result.Photos.ShouldContain(p => p.ImageFilename == "with-photographer.jpg" && p.Photographer == "Known Photographer");
        result.Photos.ShouldContain(p => p.ImageFilename == "no-photographer.jpg" && p.Photographer == null);
    }

    [Fact]
    public async Task WhenMultiplePhotos_ReturnsOrderedByCreatedOnAscending()
    {
        // Arrange — insert photos with explicit CreatedOn in descending order
        await fixture.ExecuteSqlAsync(
            $"""
            INSERT INTO dbo.Photos (MeetId, Photographer, Date, ImageFilname, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy)
            VALUES
                ({_meetId}, NULL, GETUTCDATE(), 'third.jpg',  '2024-04-03', 'test-setup', '2024-04-03', 'test-setup'),
                ({_meetId}, NULL, GETUTCDATE(), 'first.jpg',  '2024-04-01', 'test-setup', '2024-04-01', 'test-setup'),
                ({_meetId}, NULL, GETUTCDATE(), 'second.jpg', '2024-04-02', 'test-setup', '2024-04-02', 'test-setup')
            """);

        // Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{_meetSlug}/photos",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        MeetPhotos? result = await response.Content.ReadFromJsonAsync<MeetPhotos>(CancellationToken.None);
        result.ShouldNotBeNull();

        List<PhotoSummary> photos = result.Photos
            .Where(p => p.ImageFilename == "first.jpg" || p.ImageFilename == "second.jpg" || p.ImageFilename == "third.jpg")
            .ToList();

        photos.Count.ShouldBe(3);
        photos[0].ImageFilename.ShouldBe("first.jpg");
        photos[1].ImageFilename.ShouldBe("second.jpg");
        photos[2].ImageFilename.ShouldBe("third.jpg");
    }

    [Fact]
    public async Task Always_ReturnsMeetTitle()
    {
        // Arrange & Act
        HttpResponseMessage response = await _httpClient.GetAsync(
            $"{BasePath}/{_meetSlug}/photos",
            CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        MeetPhotos? result = await response.Content.ReadFromJsonAsync<MeetPhotos>(CancellationToken.None);
        result.ShouldNotBeNull();
        result.MeetTitle.ShouldNotBeNullOrWhiteSpace();
    }
}
