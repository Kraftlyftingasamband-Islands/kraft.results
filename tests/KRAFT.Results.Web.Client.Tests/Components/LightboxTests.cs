using Bunit;

using KRAFT.Results.Web.Client.Components;
using KRAFT.Results.Web.Client.Features.Meets;

using Microsoft.AspNetCore.Components;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Components;

public sealed class LightboxTests : IDisposable
{
    private readonly IReadOnlyList<GalleryPhoto> _twoPhotos =
    [
        new GalleryPhoto(
            ThumbnailUrl: "https://example.com/thumb1.jpg",
            DisplayUrl: "https://example.com/display1.jpg",
            Photographer: "Jane Doe"),
        new GalleryPhoto(
            ThumbnailUrl: "https://example.com/thumb2.jpg",
            DisplayUrl: "https://example.com/display2.jpg",
            Photographer: null),
    ];

    private readonly BunitContext _context = new();

    [Fact]
    public void DisplaysImageWithCorrectMainDisplayUrl()
    {
        // Arrange

        // Act
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 0)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".lightbox-image");
        img.GetAttribute("src").ShouldBe("https://example.com/display1.jpg");
    }

    [Fact]
    public void ShowsPhotographerAttribution_WhenPresent()
    {
        // Arrange

        // Act
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 0)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        // Assert
        cut.Find(".lightbox-photographer").TextContent.ShouldContain("Jane Doe");
    }

    [Fact]
    public void HidesPhotographerAttribution_WhenNull()
    {
        // Arrange

        // Act
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 1)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        // Assert
        cut.FindAll(".lightbox-photographer").Count.ShouldBe(0);
    }

    [Fact]
    public void ArrowRightKey_NavigatesToNextPhoto()
    {
        // Arrange
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 0)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        // Act
        cut.Find(".lightbox-backdrop").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowRight" });

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".lightbox-image");
        img.GetAttribute("src").ShouldBe("https://example.com/display2.jpg");
    }

    [Fact]
    public void ArrowLeftKey_NavigatesToPreviousPhoto_WithWrapAround()
    {
        // Arrange
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 0)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        // Act
        cut.Find(".lightbox-backdrop").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowLeft" });

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".lightbox-image");
        img.GetAttribute("src").ShouldBe("https://example.com/display2.jpg");
    }

    [Fact]
    public void EscapeKey_InvokesOnClose()
    {
        // Arrange
        bool closeCalled = false;
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 0)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true)));

        // Act
        cut.Find(".lightbox-backdrop").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Escape" });

        // Assert
        closeCalled.ShouldBeTrue();
    }

    [Fact]
    public void ShowsCorrectPhotoCounter()
    {
        // Arrange

        // Act
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 0)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        // Assert
        cut.Find(".lightbox-counter").TextContent.ShouldBe("1 / 2");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
