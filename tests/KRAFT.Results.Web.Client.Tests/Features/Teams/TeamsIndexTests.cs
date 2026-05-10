using System.Net;

using Bunit;

using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.Web.Client.Features.Teams;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Teams;

public sealed class TeamsIndexTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsLoadingStateInitially()
    {
        // Arrange
        RegisterHttpClient([], delay: true);

        // Act
        IRenderedComponent<TeamsIndex> cut = _context.Render<TeamsIndex>();

        // Assert
        cut.Find("[role='status']").ShouldNotBeNull();
        cut.Find(".visually-hidden").TextContent.ShouldBe("Sæki félög...");
    }

    [Fact]
    public void ShowsErrorWithRetryButton_WhenHttpRequestFails()
    {
        // Arrange
        RegisterFailingHttpClient();

        // Act
        IRenderedComponent<TeamsIndex> cut = _context.Render<TeamsIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find(".retry-btn").ShouldNotBeNull();
        });
    }

    [Fact]
    public void ShowsTeamCards_WhenTeamsAreLoaded()
    {
        // Arrange
        List<TeamSummary> teams =
        [
            new("thor", "Þór", "Þór", 12),
            new("kr", "Kraftlyftingafélag Reykjavíkur", "KR", 5),
        ];
        RegisterHttpClient(teams);

        // Act
        IRenderedComponent<TeamsIndex> cut = _context.Render<TeamsIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".card-grid .card").Count.ShouldBe(2);
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterHttpClient(List<TeamSummary> teams, bool delay = false)
    {
        MockHttpMessageHandler<TeamSummary> handler = new(teams, delay);
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

    private sealed class FailingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Server error");
        }
    }
}