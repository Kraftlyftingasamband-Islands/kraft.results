using Bunit;

using KRAFT.Results.Web.Client.Components;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Components;

public sealed class ClubLogoTests : IDisposable
{
    private const string ClubLogoBaseUrl = "https://example.blob.core.windows.net/images";

    private readonly BunitContext _context = new();

    public ClubLogoTests()
    {
        RegisterConfiguration();
    }

    [Fact]
    public void WhenFilenameIsNull_RendersSvgPlaceholder()
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p.Add(c => c.Size, ClubLogoSize.Medium));

        // Assert
        cut.Find(".team-logo svg").ShouldNotBeNull();
        cut.FindAll(".team-logo img").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenFilenameIsProvided_RendersImg()
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p
                .Add(c => c.Filename, "thor.png")
                .Add(c => c.Size, ClubLogoSize.Medium));

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".team-logo img");
        img.GetAttribute("src").ShouldBe($"{ClubLogoBaseUrl}/thor.png?width=64&height=64&crop=auto");
        img.GetAttribute("loading").ShouldBe("lazy");
    }

    [Fact]
    public void WhenAltNotProvided_DefaultsToEmpty()
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p
                .Add(c => c.Filename, "thor.png")
                .Add(c => c.Size, ClubLogoSize.Medium));

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".team-logo img");
        img.GetAttribute("alt").ShouldBe(string.Empty);
    }

    [Fact]
    public void WhenAltIsProvided_RendersAltText()
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p
                .Add(c => c.Filename, "thor.png")
                .Add(c => c.Alt, "Þór IF")
                .Add(c => c.Size, ClubLogoSize.Large));

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".team-logo img");
        img.GetAttribute("alt").ShouldBe("Þór IF");
    }

    [Theory]
    [InlineData(ClubLogoSize.Small, "team-logo--sm")]
    [InlineData(ClubLogoSize.Medium, "team-logo--md")]
    [InlineData(ClubLogoSize.Large, "team-logo--lg")]
    public void WhenSizeIsSet_AppliesSizeClass(ClubLogoSize size, string expectedClass)
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p.Add(c => c.Size, size));

        // Assert
        cut.Find($".team-logo.{expectedClass}").ShouldNotBeNull();
    }

    [Theory]
    [InlineData(ClubLogoSize.Small)]
    [InlineData(ClubLogoSize.Medium)]
    public void WhenSizeIsSmallOrMedium_SrcQueryStringIsPinned64x64Crop(ClubLogoSize size)
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p
                .Add(c => c.Filename, "thor.png")
                .Add(c => c.Size, size));

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".team-logo img");
        img.GetAttribute("src").ShouldBe($"{ClubLogoBaseUrl}/thor.png?width=64&height=64&crop=auto");
    }

    [Fact]
    public void WhenSizeIsLarge_SrcQueryStringIsPinned128x128()
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p
                .Add(c => c.Filename, "thor.png")
                .Add(c => c.Size, ClubLogoSize.Large));

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".team-logo img");
        img.GetAttribute("src").ShouldBe($"{ClubLogoBaseUrl}/thor.png?width=128&height=128");
    }

    [Theory]
    [InlineData(ClubLogoSize.Small)]
    [InlineData(ClubLogoSize.Medium)]
    [InlineData(ClubLogoSize.Large)]
    public void WhenAnySize_DoesNotEmitSrcset(ClubLogoSize size)
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p
                .Add(c => c.Filename, "thor.png")
                .Add(c => c.Size, size));

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".team-logo img");
        img.GetAttribute("srcset").ShouldBeNull();
    }

    [Fact]
    public void WhenFallbackTextSetAndFilenameIsNull_RendersFallbackText()
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p
                .Add(c => c.FallbackText, "ÞOR")
                .Add(c => c.Size, ClubLogoSize.Medium));

        // Assert
        cut.Find(".team-logo-placeholder").TextContent.Trim().ShouldBe("ÞOR");
        cut.FindAll(".team-logo svg").Count.ShouldBe(0);
        cut.FindAll(".team-logo img").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenFallbackTextIsNotSet_RendersShieldSvg()
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p.Add(c => c.Size, ClubLogoSize.Medium));

        // Assert
        cut.Find(".team-logo svg").ShouldNotBeNull();
    }

    [Fact]
    public void WhenFallbackTextSetAndFilenameIsValid_RendersImgAndHiddenFallbackTextSibling()
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p
                .Add(c => c.Filename, "thor.png")
                .Add(c => c.FallbackText, "ÞOR")
                .Add(c => c.Size, ClubLogoSize.Medium));

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".team-logo img");
        img.ShouldNotBeNull();

        string onerror = img.GetAttribute("onerror").ShouldNotBeNull();
        onerror.ShouldContain("this.nextElementSibling.style.display='flex'");

        AngleSharp.Dom.IElement placeholder = cut.Find(".team-logo-placeholder");
        placeholder.GetAttribute("style").ShouldNotBeNull().ShouldContain("display:none");
        placeholder.TextContent.Trim().ShouldBe("ÞOR");
        cut.FindAll(".team-logo svg").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenFallbackTextIsSet_FallbackTextPlaceholderIsNotAriaHidden()
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p
                .Add(c => c.FallbackText, "ÞOR")
                .Add(c => c.Size, ClubLogoSize.Medium));

        // Assert
        AngleSharp.Dom.IElement placeholder = cut.Find(".team-logo-placeholder");
        placeholder.GetAttribute("aria-hidden").ShouldBeNull();
    }

    [Fact]
    public void WhenFallbackTextIsNotSet_ShieldPlaceholderIsAriaHidden()
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p.Add(c => c.Size, ClubLogoSize.Medium));

        // Assert
        AngleSharp.Dom.IElement placeholder = cut.Find(".team-logo-placeholder");
        placeholder.GetAttribute("aria-hidden").ShouldBe("true");
    }

    [Fact]
    public void WhenSizeIsSmallAndFallbackTextIsSet_AppliesTextModifierClass()
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p
                .Add(c => c.FallbackText, "ÞOR")
                .Add(c => c.Size, ClubLogoSize.Small));

        // Assert
        cut.Find(".team-logo.team-logo--sm.team-logo--text").ShouldNotBeNull();
    }

    [Fact]
    public void WhenFallbackTextSetAndSizeIsMedium_EmitsTextModifierClass()
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p
                .Add(c => c.FallbackText, "ÞOR")
                .Add(c => c.Size, ClubLogoSize.Medium));

        // Assert
        cut.Find(".team-logo.team-logo--md.team-logo--text").ShouldNotBeNull();
    }

    [Fact]
    public void WhenFallbackTextIsNotSet_DoesNotApplyTextModifierClass()
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p.Add(c => c.Size, ClubLogoSize.Small));

        // Assert
        cut.FindAll(".team-logo--text").Count.ShouldBe(0);
    }

    [Theory]
    [InlineData("evil,logo.png")]
    [InlineData("evil logo.png")]
    [InlineData("../evil.png")]
    [InlineData("subdir/logo.png")]
    [InlineData(@"C:\logos\evil.png")]
    [InlineData("http://evil.com/logo.png")]
    [InlineData("//evil.com/logo.png")]
    [InlineData("javascript:alert(1)")]
    [InlineData("logo.png?width=999")]
    [InlineData("logo.png#section")]
    [InlineData("logo%2Fevil.png")]
    public void WhenFallbackTextSetAndFilenameIsUnsafe_RendersFallbackText(string unsafeFilename)
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p
                .Add(c => c.Filename, unsafeFilename)
                .Add(c => c.FallbackText, "ÞOR")
                .Add(c => c.Size, ClubLogoSize.Medium));

        // Assert
        cut.Find(".team-logo-placeholder").TextContent.Trim().ShouldBe("ÞOR");
        cut.FindAll(".team-logo svg").Count.ShouldBe(0);
        cut.FindAll(".team-logo img").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenFilenameContainsComma_ShowsPlaceholder()
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p
                .Add(c => c.Filename, "evil,logo.png")
                .Add(c => c.Size, ClubLogoSize.Medium));

        // Assert
        cut.Find(".team-logo svg").ShouldNotBeNull();
        cut.FindAll(".team-logo img").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenFilenameContainsSpace_ShowsPlaceholder()
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p
                .Add(c => c.Filename, "evil logo.png")
                .Add(c => c.Size, ClubLogoSize.Medium));

        // Assert
        cut.Find(".team-logo svg").ShouldNotBeNull();
        cut.FindAll(".team-logo img").Count.ShouldBe(0);
    }

    [Theory]
    [InlineData("../evil.png")]
    [InlineData("subdir/logo.png")]
    [InlineData("C:\\logos\\evil.png")]
    [InlineData("http://evil.com/logo.png")]
    [InlineData("//evil.com/logo.png")]
    [InlineData("javascript:alert(1)")]
    [InlineData("logo.png?width=999")]
    [InlineData("logo.png#section")]
    [InlineData("logo%2Fevil.png")]
    public void WhenFilenameIsInvalid_ShowsPlaceholder(string invalidFilename)
    {
        // Arrange

        // Act
        IRenderedComponent<ClubLogo> cut = _context.Render<ClubLogo>(
            p => p
                .Add(c => c.Filename, invalidFilename)
                .Add(c => c.Size, ClubLogoSize.Medium));

        // Assert
        cut.Find(".team-logo svg").ShouldNotBeNull();
        cut.FindAll(".team-logo img").Count.ShouldBe(0);
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
                ["ImageBaseUrl"] = ClubLogoBaseUrl,
            })
            .Build();
        _context.Services.AddSingleton(configuration);
    }
}
