using Bunit;

using KRAFT.Results.Contracts.TeamCompetition;
using KRAFT.Results.Web.Client.Features.TeamCompetition;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.TeamCompetition;

public sealed class StandingsCardTests : IDisposable
{
    private const string TeamLogoBaseUrl = "https://example.blob.core.windows.net/images";

    private readonly BunitContext _context = new();

    [Fact]
    public void WhenRankIs1_CardHasSCCardFirstClass()
    {
        // Arrange
        TeamCompetitionStanding standing = new(1, "Þór IF", "Þór", "thor", null, 100);
        RegisterServices();

        // Act
        IRenderedComponent<StandingsCard> cut = _context.Render<StandingsCard>(
            parameters => parameters.Add(p => p.Standing, standing));

        // Assert
        cut.Find(".sc-card-first").ShouldNotBeNull();
    }

    [Fact]
    public void WhenRankIsNot1_CardDoesNotHaveSCCardFirstClass()
    {
        // Arrange
        TeamCompetitionStanding standing = new(2, "KR", "KR", "kr", null, 80);
        RegisterServices();

        // Act
        IRenderedComponent<StandingsCard> cut = _context.Render<StandingsCard>(
            parameters => parameters.Add(p => p.Standing, standing));

        // Assert
        cut.FindAll(".sc-card-first").Count.ShouldBe(0);
    }

    [Fact]
    public void WhenTeamSlugIsSet_NameRendersAsLink()
    {
        // Arrange
        TeamCompetitionStanding standing = new(1, "Þór IF", "Þór", "thor", null, 100);
        RegisterServices();

        // Act
        IRenderedComponent<StandingsCard> cut = _context.Render<StandingsCard>(
            parameters => parameters.Add(p => p.Standing, standing));

        // Assert
        AngleSharp.Dom.IElement link = cut.Find("a.esc-name");
        link.GetAttribute("href").ShouldBe("/teams/thor");
        link.TextContent.ShouldBe("Þór");
    }

    [Fact]
    public void WhenTeamSlugIsNull_NameRendersAsPlainText()
    {
        // Arrange
        TeamCompetitionStanding standing = new(1, "Þór IF", "Þór", null, null, 100);
        RegisterServices();

        // Act
        IRenderedComponent<StandingsCard> cut = _context.Render<StandingsCard>(
            parameters => parameters.Add(p => p.Standing, standing));

        // Assert
        cut.FindAll("a.esc-name").Count.ShouldBe(0);
        cut.Find("span.esc-name").TextContent.ShouldBe("Þór");
    }

    [Fact]
    public void WhenTeamSlugIsEmpty_NameRendersAsPlainText()
    {
        // Arrange
        TeamCompetitionStanding standing = new(1, "Þór IF", "Þór", string.Empty, null, 100);
        RegisterServices();

        // Act
        IRenderedComponent<StandingsCard> cut = _context.Render<StandingsCard>(
            parameters => parameters.Add(p => p.Standing, standing));

        // Assert
        cut.FindAll("a.esc-name").Count.ShouldBe(0);
        cut.Find("span.esc-name").TextContent.ShouldBe("Þór");
    }

    [Fact]
    public void WhenRendered_ShowsStigLabel()
    {
        // Arrange
        TeamCompetitionStanding standing = new(1, "Þór IF", "Þór", "thor", null, 100);
        RegisterServices();

        // Act
        IRenderedComponent<StandingsCard> cut = _context.Render<StandingsCard>(
            parameters => parameters.Add(p => p.Standing, standing));

        // Assert
        cut.Find(".esc-label").TextContent.ShouldBe("Stig");
    }

    [Fact]
    public void WhenRendered_ShowsTotalPointsAsValue()
    {
        // Arrange
        TeamCompetitionStanding standing = new(2, "KR", "KR", "kr", null, 85);
        RegisterServices();

        // Act
        IRenderedComponent<StandingsCard> cut = _context.Render<StandingsCard>(
            parameters => parameters.Add(p => p.Standing, standing));

        // Assert
        cut.Find(".esc-value").TextContent.Trim().ShouldBe("85");
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private void RegisterServices()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ImageBaseUrl"] = TeamLogoBaseUrl,
            })
            .Build();
        _context.Services.AddSingleton(configuration);
        _context.AddAuthorization();
    }
}
