using System.Net;
using System.Net.Http.Json;

using Bunit;

using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Web.Client.Features.Meets;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Meets;

public sealed class GalleryPageTests : IDisposable
{
    private const string ImageBaseUrl = "https://example.blob.core.windows.net/images";

    private readonly BunitContext _context = new();

    [Fact]
    public void RendersThumbnailGrid_WhenPhotosExist()
    {
        // Arrange
        List<PhotoSummary> photos =
        [
            new(ImageFilename: "photo1.jpg", Photographer: null),
            new(ImageFilename: "photo2.jpg", Photographer: "Jane Doe"),
        ];
        RegisterHttpClient(photos);

        // Act
        IRenderedComponent<GalleryPage> cut = _context.Render<GalleryPage>(
            p => p.Add(c => c.Slug, "test-meet"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find(".gallery-grid").ShouldNotBeNull();
            cut.FindAll(".gallery-thumb").Count.ShouldBe(2);
        });
    }

    [Fact]
    public void ShowsEmptyState_WhenNoPhotos()
    {
        // Arrange
        RegisterHttpClient([]);

        // Act
        IRenderedComponent<GalleryPage> cut = _context.Render<GalleryPage>(
            p => p.Add(c => c.Slug, "test-meet"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find(".empty-state").TextContent.ShouldContain("Engar myndir fundust.");
        });
    }

    [Fact]
    public void ThumbnailSrcs_ContainThumbnailQueryString()
    {
        // Arrange
        List<PhotoSummary> photos =
        [
            new(ImageFilename: "myphoto.jpg", Photographer: null),
        ];
        RegisterHttpClient(photos);

        // Act
        IRenderedComponent<GalleryPage> cut = _context.Render<GalleryPage>(
            p => p.Add(c => c.Slug, "test-meet"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            AngleSharp.Dom.IElement img = cut.Find(".gallery-thumb img");
            img.GetAttribute("src").ShouldBe($"{ImageBaseUrl}/myphoto.jpg{MeetPhotoSizes.Thumbnail}");
        });
    }

    [Theory]
    [InlineData("../traversal.jpg")]
    [InlineData("//cdn.evil.com/img.jpg")]
    [InlineData("has/slash.jpg")]
    [InlineData("has\\backslash.jpg")]
    [InlineData("has:colon.jpg")]
    [InlineData("has?query.jpg")]
    [InlineData("has#hash.jpg")]
    public void SkipsPhotos_WithInvalidFilenames(string invalidFilename)
    {
        // Arrange
        List<PhotoSummary> photos =
        [
            new(ImageFilename: "valid.jpg", Photographer: null),
            new(ImageFilename: invalidFilename, Photographer: null),
        ];
        RegisterHttpClient(photos);

        // Act
        IRenderedComponent<GalleryPage> cut = _context.Render<GalleryPage>(
            p => p.Add(c => c.Slug, "test-meet"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".gallery-thumb").Count.ShouldBe(1);
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterHttpClient(List<PhotoSummary> photos)
    {
        GalleryPageMockHandler handler = new(photos);
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        RegisterConfiguration();
        _context.AddAuthorization();
    }

    private void RegisterConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ImageBaseUrl"] = ImageBaseUrl,
            })
            .Build();
        _context.Services.AddSingleton(configuration);
    }

    private sealed class GalleryPageMockHandler(List<PhotoSummary> photos) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            MeetPhotos response = new("Test Meet", photos);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response),
            });
        }
    }
}
