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

    [Fact]
    public void ShowsAriaLiveRegion_AroundPhotoInfo()
    {
        // Act
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 0)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        // Assert
        AngleSharp.Dom.IElement liveRegion = cut.Find("[aria-live='polite']");
        liveRegion.GetAttribute("aria-atomic").ShouldBe("true");
        liveRegion.QuerySelector(".lightbox-info").ShouldNotBeNull();
    }

    [Fact]
    public void FallsBackToThumbnailUrl_WhenDisplayUrlFails()
    {
        // Arrange
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 0)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        cut.Find(".lightbox-image").GetAttribute("src").ShouldBe("https://example.com/display1.jpg");

        // Act
        cut.Find(".lightbox-image").TriggerEvent("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".lightbox-image");
        img.GetAttribute("src").ShouldBe("https://example.com/thumb1.jpg");
    }

    [Fact]
    public void ShowsErrorMessage_WhenBothDisplayAndThumbnailFail()
    {
        // Arrange
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 0)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        // Act — first error triggers fallback, second error shows message
        cut.Find(".lightbox-image").TriggerEvent("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());
        cut.Find(".lightbox-image").TriggerEvent("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());

        // Assert
        cut.FindAll(".lightbox-image").Count.ShouldBe(0);
        cut.Find(".lightbox-error-message").TextContent.ShouldContain("Ekki tókst að hlaða mynd");
    }

    [Fact]
    public void ResetsErrorState_WhenNavigatingToNextPhoto()
    {
        // Arrange
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 0)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        cut.Find(".lightbox-image").TriggerEvent("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());
        cut.Find(".lightbox-image").TriggerEvent("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());
        cut.FindAll(".lightbox-image").Count.ShouldBe(0);

        // Act
        cut.Find(".lightbox-backdrop").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "ArrowRight" });

        // Assert
        cut.FindAll(".lightbox-image").Count.ShouldBe(1);
        cut.Find(".lightbox-image").GetAttribute("src").ShouldBe("https://example.com/display2.jpg");
        cut.FindAll(".lightbox-error-message").Count.ShouldBe(0);
    }

    [Fact]
    public void SwipeLeft_NavigatesToNextPhoto()
    {
        // Arrange
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 0)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        Microsoft.AspNetCore.Components.Web.TouchEventArgs touchStart = new()
        {
            ChangedTouches = [new Microsoft.AspNetCore.Components.Web.TouchPoint { ClientX = 200 }],
        };
        Microsoft.AspNetCore.Components.Web.TouchEventArgs touchEnd = new()
        {
            ChangedTouches = [new Microsoft.AspNetCore.Components.Web.TouchPoint { ClientX = 100 }],
        };

        // Act
        cut.Find(".lightbox-content").TriggerEvent("ontouchstart", touchStart);
        cut.Find(".lightbox-content").TriggerEvent("ontouchend", touchEnd);

        // Assert
        cut.Find(".lightbox-image").GetAttribute("src").ShouldBe("https://example.com/display2.jpg");
    }

    [Fact]
    public void SwipeRight_NavigatesToPreviousPhoto()
    {
        // Arrange
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 1)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        Microsoft.AspNetCore.Components.Web.TouchEventArgs touchStart = new()
        {
            ChangedTouches = [new Microsoft.AspNetCore.Components.Web.TouchPoint { ClientX = 100 }],
        };
        Microsoft.AspNetCore.Components.Web.TouchEventArgs touchEnd = new()
        {
            ChangedTouches = [new Microsoft.AspNetCore.Components.Web.TouchPoint { ClientX = 200 }],
        };

        // Act
        cut.Find(".lightbox-content").TriggerEvent("ontouchstart", touchStart);
        cut.Find(".lightbox-content").TriggerEvent("ontouchend", touchEnd);

        // Assert
        cut.Find(".lightbox-image").GetAttribute("src").ShouldBe("https://example.com/display1.jpg");
    }

    [Fact]
    public void ShortSwipe_DoesNotNavigate()
    {
        // Arrange
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 0)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        Microsoft.AspNetCore.Components.Web.TouchEventArgs touchStart = new()
        {
            ChangedTouches = [new Microsoft.AspNetCore.Components.Web.TouchPoint { ClientX = 200 }],
        };
        Microsoft.AspNetCore.Components.Web.TouchEventArgs touchEnd = new()
        {
            ChangedTouches = [new Microsoft.AspNetCore.Components.Web.TouchPoint { ClientX = 170 }],
        };

        // Act
        cut.Find(".lightbox-content").TriggerEvent("ontouchstart", touchStart);
        cut.Find(".lightbox-content").TriggerEvent("ontouchend", touchEnd);

        // Assert
        cut.Find(".lightbox-image").GetAttribute("src").ShouldBe("https://example.com/display1.jpg");
    }

    [Fact]
    public void WhenOpen_RendersImageContainer()
    {
        // Act
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 0)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        // Assert
        cut.Find(".lightbox-image-container").ShouldNotBeNull();
    }

    [Fact]
    public void ImageContainer_GetsLoadedClass_WhenImageLoads()
    {
        // Arrange
        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, _twoPhotos)
                .Add(c => c.StartIndex, 0)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        cut.Find(".lightbox-image-container").ClassList.ShouldNotContain("is-loaded");

        // Act
        cut.Find(".lightbox-image").TriggerEvent("onload", new Microsoft.AspNetCore.Components.Web.ProgressEventArgs());

        // Assert
        cut.Find(".lightbox-image-container").ClassList.ShouldContain("is-loaded");
    }

    [Fact]
    public void TabKey_WithSinglePhoto_DoesNotThrow_AndCyclesToCloseButton()
    {
        // Arrange
        IReadOnlyList<GalleryPhoto> onePhoto =
        [
            new GalleryPhoto(
                ThumbnailUrl: "https://example.com/thumb1.jpg",
                DisplayUrl: "https://example.com/display1.jpg",
                Photographer: null),
        ];

        IRenderedComponent<Lightbox> cut = _context.Render<Lightbox>(
            p => p
                .Add(c => c.Photos, onePhoto)
                .Add(c => c.StartIndex, 0)
                .Add(c => c.IsOpen, true)
                .Add(c => c.OnClose, EventCallback.Empty));

        // Act & Assert — pressing Tab must not throw a JS interop exception
        Should.NotThrow(() =>
            cut.Find(".lightbox-backdrop").KeyDown(
                new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Tab" }));
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
