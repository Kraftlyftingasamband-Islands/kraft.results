using Bunit;

using KRAFT.Results.Web.Client.Components;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Components;

public sealed class TeamLogoTests : IDisposable
{
    private const string TeamLogoBaseUrl = "https://example.blob.core.windows.net/images";

    private readonly BunitContext _context = new();

    public TeamLogoTests()
    {
        RegisterConfiguration();
    }

    [Fact]
    public void RendersSvgPlaceholder_WhenFilenameIsNull()
    {
        // Arrange

        // Act
        IRenderedComponent<TeamLogo> cut = _context.Render<TeamLogo>(
            p => p.Add(c => c.Size, TeamLogoSize.Medium));

        // Assert
        cut.Find(".team-logo svg").ShouldNotBeNull();
        cut.FindAll(".team-logo img").Count.ShouldBe(0);
    }

    [Fact]
    public void RendersImg_WhenFilenameIsProvided()
    {
        // Arrange

        // Act
        IRenderedComponent<TeamLogo> cut = _context.Render<TeamLogo>(
            p => p
                .Add(c => c.Filename, "thor.png")
                .Add(c => c.Size, TeamLogoSize.Medium));

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".team-logo img");
        img.GetAttribute("src").ShouldBe($"{TeamLogoBaseUrl}/thor.png?width=96&height=96");
        img.GetAttribute("loading").ShouldBe("lazy");
    }

    [Fact]
    public void AltDefaultsToEmpty_ForDecorativeUse()
    {
        // Arrange

        // Act
        IRenderedComponent<TeamLogo> cut = _context.Render<TeamLogo>(
            p => p
                .Add(c => c.Filename, "thor.png")
                .Add(c => c.Size, TeamLogoSize.Medium));

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".team-logo img");
        img.GetAttribute("alt").ShouldBe(string.Empty);
    }

    [Fact]
    public void RendersAltText_WhenAltIsProvided()
    {
        // Arrange

        // Act
        IRenderedComponent<TeamLogo> cut = _context.Render<TeamLogo>(
            p => p
                .Add(c => c.Filename, "thor.png")
                .Add(c => c.Alt, "Þór IF")
                .Add(c => c.Size, TeamLogoSize.Large));

        // Assert
        AngleSharp.Dom.IElement img = cut.Find(".team-logo img");
        img.GetAttribute("alt").ShouldBe("Þór IF");
    }

    [Theory]
    [InlineData(TeamLogoSize.Small, "team-logo--sm")]
    [InlineData(TeamLogoSize.Medium, "team-logo--md")]
    [InlineData(TeamLogoSize.Large, "team-logo--lg")]
    public void AppliesSizeClass_ForEachSize(TeamLogoSize size, string expectedClass)
    {
        // Arrange

        // Act
        IRenderedComponent<TeamLogo> cut = _context.Render<TeamLogo>(
            p => p.Add(c => c.Size, size));

        // Assert
        cut.Find($".team-logo.{expectedClass}").ShouldNotBeNull();
    }

    [Theory]
    [InlineData("../evil.png")]
    [InlineData("subdir/logo.png")]
    [InlineData("C:\\logos\\evil.png")]
    [InlineData("http://evil.com/logo.png")]
    [InlineData("//evil.com/logo.png")]
    [InlineData("javascript:alert(1)")]
    public void ShowsPlaceholder_WhenFilenameIsInvalid(string invalidFilename)
    {
        // Arrange

        // Act
        IRenderedComponent<TeamLogo> cut = _context.Render<TeamLogo>(
            p => p
                .Add(c => c.Filename, invalidFilename)
                .Add(c => c.Size, TeamLogoSize.Medium));

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
                ["TeamLogoBaseUrl"] = TeamLogoBaseUrl,
            })
            .Build();
        _context.Services.AddSingleton(configuration);
    }
}