using Bunit;

using KRAFT.Results.Web.Client.Components;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Components;

public sealed class AthletePhotoTests : IDisposable
{
    private const string ImageBaseUrl = "https://example.blob.core.windows.net/images";

    private readonly BunitContext _context = new();

    public AthletePhotoTests()
    {
        RegisterConfiguration();
    }

    [Fact]
    public void RendersSvgPlaceholder_WhenFilenameIsNull()
    {
        // Arrange

        // Act
        IRenderedComponent<AthletePhoto> cut = _context.Render<AthletePhoto>(
            p => p.Add(c => c.Size, AthletePhotoSize.Small));

        // Assert
        cut.Find(".athlete-photo svg").ShouldNotBeNull();
        cut.FindAll(".athlete-photo img").Count.ShouldBe(0);
    }

    [Fact]
    public void RendersSvgPlaceholder_WhenFilenameIsEmpty()
    {
        // Arrange

        // Act
        IRenderedComponent<AthletePhoto> cut = _context.Render<AthletePhoto>(
            p => p
                .Add(c => c.Filename, string.Empty)
                .Add(c => c.Size, AthletePhotoSize.Small));

        // Assert
        cut.Find(".athlete-photo svg").ShouldNotBeNull();
        cut.FindAll(".athlete-photo img").Count.ShouldBe(0);
    }

    [Fact]
    public void RendersImg_WithSmallQueryString_WhenSizeIsSmall()
    {
        // Arrange

        // Act
        IRenderedComponent<AthletePhoto> cut = _context.Render<AthletePhoto>(
            p => p
                .Add(c => c.Filename, "jondoe.jpg")
                .Add(c => c.Size, AthletePhotoSize.Small));

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".athlete-photo img");
        img.GetAttribute("src").ShouldBe($"{ImageBaseUrl}/jondoe.jpg?width=64&height=64&crop=auto");
    }

    [Fact]
    public void RendersImg_WithLargeQueryString_WhenSizeIsLarge()
    {
        // Arrange

        // Act
        IRenderedComponent<AthletePhoto> cut = _context.Render<AthletePhoto>(
            p => p
                .Add(c => c.Filename, "jondoe.jpg")
                .Add(c => c.Size, AthletePhotoSize.Large));

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".athlete-photo img");
        img.GetAttribute("src").ShouldBe($"{ImageBaseUrl}/jondoe.jpg?width=300&height=200&crop=auto");
    }

    [Fact]
    public void RendersAltText_WhenAltIsProvided()
    {
        // Arrange

        // Act
        IRenderedComponent<AthletePhoto> cut = _context.Render<AthletePhoto>(
            p => p
                .Add(c => c.Filename, "jondoe.jpg")
                .Add(c => c.Alt, "Jón Döe")
                .Add(c => c.Size, AthletePhotoSize.Small));

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".athlete-photo img");
        img.GetAttribute("alt").ShouldBe("Jón Döe");
    }

    [Theory]
    [InlineData(AthletePhotoSize.Small, "athlete-photo--sm")]
    [InlineData(AthletePhotoSize.Large, "athlete-photo--lg")]
    public void AppliesSizeClass_ForEachSize(AthletePhotoSize size, string expectedClass)
    {
        // Arrange

        // Act
        IRenderedComponent<AthletePhoto> cut = _context.Render<AthletePhoto>(
            p => p.Add(c => c.Size, size));

        // Assert
        cut.Find($".athlete-photo.{expectedClass}").ShouldNotBeNull();
    }

    [Theory]
    [InlineData("../evil.png")]
    [InlineData("subdir/photo.png")]
    [InlineData("C:\\photos\\evil.png")]
    [InlineData("http://evil.com/photo.png")]
    [InlineData("//evil.com/photo.png")]
    [InlineData("javascript:alert(1)")]
    [InlineData("photo.png?width=999")]
    [InlineData("photo.png#section")]
    public void ShowsPlaceholder_WhenFilenameContainsPathTraversalCharacters(string invalidFilename)
    {
        // Arrange

        // Act
        IRenderedComponent<AthletePhoto> cut = _context.Render<AthletePhoto>(
            p => p
                .Add(c => c.Filename, invalidFilename)
                .Add(c => c.Size, AthletePhotoSize.Small));

        // Assert
        cut.Find(".athlete-photo svg").ShouldNotBeNull();
        cut.FindAll(".athlete-photo img").Count.ShouldBe(0);
    }

    [Fact]
    public void UsesImageBaseUrlFromConfiguration()
    {
        // Arrange

        // Act
        IRenderedComponent<AthletePhoto> cut = _context.Render<AthletePhoto>(
            p => p
                .Add(c => c.Filename, "jondoe.jpg")
                .Add(c => c.Size, AthletePhotoSize.Small));

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".athlete-photo img");
        img.GetAttribute("src").ShouldStartWith(ImageBaseUrl);
    }

    public void Dispose()
    {
        _context.Dispose();
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
}
