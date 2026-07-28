using System.Globalization;
using System.Net;
using System.Net.Http.Json;

using Bunit;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Rankings;
using KRAFT.Results.Web.Client.Features.Rankings;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Rankings;

public sealed class RankingsIndexTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsLoadingSpinner_WhenDataIsBeingFetched()
    {
        // Arrange
        RegisterHttpClient(MakeResponse(), delay: true);

        // Act
        IRenderedComponent<RankingsIndex> cut = _context.Render<RankingsIndex>();

        // Assert
        cut.Find("[role='status']").ShouldNotBeNull();
        cut.Find(".visually-hidden").TextContent.ShouldBe("Sæki stigatöflur...");
    }

    [Fact]
    public void ShowsErrorWithRetryButton_WhenHttpRequestFails()
    {
        // Arrange
        RegisterFailingHttpClient();

        // Act
        IRenderedComponent<RankingsIndex> cut = _context.Render<RankingsIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find(".retry-btn").ShouldNotBeNull();
        });
    }

    [Fact]
    public void ShowsRankingCards_WhenDataIsLoaded()
    {
        // Arrange
        PagedResponse<RankingEntry> response = MakeResponse(items:
        [
            new(1, "Jón Jónsson", "jon-jonsson", "m", 200m, "83", 80m, null, 150m, "nationals-2024", true, new DateOnly(2024, 3, 15)),
            new(2, "Sigríður Sigurðardóttir", "sigridur-sigurdardottir", "f", 180m, "63", 60m, null, 140m, "nationals-2024", true, new DateOnly(2024, 3, 15)),
        ]);
        RegisterHttpClient(response);

        // Act
        IRenderedComponent<RankingsIndex> cut = _context.Render<RankingsIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".rankings-list .card").Count.ShouldBe(2);
        });
    }

    [Fact]
    public void FilterBarRemainsVisible_WhileLoading()
    {
        // Arrange
        RegisterHttpClient(MakeResponse(), delay: true);

        // Act
        IRenderedComponent<RankingsIndex> cut = _context.Render<RankingsIndex>();

        // Assert — filter bar is outside DataLoader and always renders
        cut.Find(".filter-bar").ShouldNotBeNull();
    }

    [Fact]
    public void WhenRendered_AthleteLinkAndPillsAppear()
    {
        // Arrange
        PagedResponse<RankingEntry> response = MakeResponse(items:
        [
            new(3, "Gunnar Gunnarsson", "gunnar-gunnarsson", "m", 320m, "93", 91.5m, 450.25m, 380m, "spring-2024", true, new DateOnly(2024, 5, 1)),
        ]);
        RegisterHttpClient(response);

        // Act
        IRenderedComponent<RankingsIndex> cut = _context.Render<RankingsIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            AngleSharp.Dom.IElement nameLink = cut.Find("a.esc-name");
            nameLink.GetAttribute("href").ShouldBe("/athletes/gunnar-gunnarsson");
            nameLink.TextContent.ShouldBe("Gunnar Gunnarsson");

            cut.FindAll(".esc-pill").Count.ShouldBe(2);
        });
    }

    [Fact]
    public void WhenIpfPointsHasValue_ValueRendersAsLinkToMeetWithAriaLabel()
    {
        // Arrange
        decimal ipfPoints = 450.25m;
        string formatted = ipfPoints.ToString("F2", CultureInfo.CurrentCulture);
        PagedResponse<RankingEntry> response = MakeResponse(items:
        [
            new(1, "Gunnar Gunnarsson", "gunnar-gunnarsson", "m", 320m, "93", 91.5m, ipfPoints, 380m, "spring-2024", true, new DateOnly(2024, 5, 1)),
        ]);
        RegisterHttpClient(response);

        // Act
        IRenderedComponent<RankingsIndex> cut = _context.Render<RankingsIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            AngleSharp.Dom.IElement valueLink = cut.Find("a.esc-value");
            valueLink.GetAttribute("href").ShouldBe("/meets/spring-2024");
            valueLink.GetAttribute("aria-label").ShouldBe($"{formatted} IPF stig, opna mót");
            valueLink.TextContent.Trim().ShouldBe(formatted);
        });
    }

    [Fact]
    public void WhenIpfPointsIsNull_ValueRendersDash()
    {
        // Arrange
        PagedResponse<RankingEntry> response = MakeResponse(items:
        [
            new(5, "Jón Jónsson", "jon-jonsson", "m", 200m, "83", 80m, null, 150m, "nationals-2024", true, new DateOnly(2024, 3, 15)),
        ]);
        RegisterHttpClient(response);

        // Act
        IRenderedComponent<RankingsIndex> cut = _context.Render<RankingsIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("span.esc-value").TextContent.Trim().ShouldBe("-");
        });
    }

    [Fact]
    public void WhenRendered_MetaTextContainsResultAndDisciplineLabel()
    {
        // Arrange
        decimal result = 200.5m;
        string formattedResult = result.ToString("F1", CultureInfo.CurrentCulture);
        PagedResponse<RankingEntry> response = MakeResponse(items:
        [
            new(1, "Jón Jónsson", "jon-jonsson", "m", result, "83", 80m, 350m, 150m, "nationals-2024", true, new DateOnly(2024, 3, 15)),
        ]);
        RegisterHttpClient(response);

        // Act
        IRenderedComponent<RankingsIndex> cut = _context.Render<RankingsIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find(".esc-meta").TextContent.ShouldContain(formattedResult);
        });
    }

    [Fact]
    public void WhenRendered_LabelIsIpfStig()
    {
        // Arrange
        PagedResponse<RankingEntry> response = MakeResponse(items:
        [
            new(1, "Jón Jónsson", "jon-jonsson", "m", 200m, "83", 80m, 350m, 150m, "nationals-2024", true, new DateOnly(2024, 3, 15)),
        ]);
        RegisterHttpClient(response);

        // Act
        IRenderedComponent<RankingsIndex> cut = _context.Render<RankingsIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find(".esc-label").TextContent.ShouldBe("IPF stig");
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static PagedResponse<RankingEntry> MakeResponse(IReadOnlyList<RankingEntry>? items = null) =>
        new(
            Items: items ?? [],
            Page: 1,
            PageSize: 50,
            TotalCount: items?.Count ?? 0);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterHttpClient(PagedResponse<RankingEntry> response, bool delay = false)
    {
        RankingsMockHandler handler = new(response, delay);
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterFailingHttpClient()
    {
        FailingHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
    }

    private sealed class RankingsMockHandler(PagedResponse<RankingEntry> response, bool delay = false) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (delay)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(response),
            };
        }
    }
}
