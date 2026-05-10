using System.Net;
using System.Net.Http.Json;

using Bunit;

using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.Web.Client.Features.Teams;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Teams;

public sealed class TeamDetailsPageTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsLoadingStateInitially()
    {
        // Arrange
        RegisterHttpClient(new TeamDetails("thor", "Þór", "Þór", "Þór IF", "ISL", []), delay: true);

        // Act
        IRenderedComponent<TeamDetailsPage> cut = _context.Render<TeamDetailsPage>(
            p => p.Add(c => c.Slug, "thor"));

        // Assert
        cut.Find("[role='status']").ShouldNotBeNull();
        cut.Find(".visually-hidden").TextContent.ShouldBe("Sæki félag...");
    }

    [Fact]
    public void ShowsNotFoundMessage_WhenTeamReturnsNull()
    {
        // Arrange
        RegisterNullHttpClient();

        // Act
        IRenderedComponent<TeamDetailsPage> cut = _context.Render<TeamDetailsPage>(
            p => p.Add(c => c.Slug, "nonexistent"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find("[role='alert']").TextContent.ShouldContain("félag");
            cut.Find("[role='alert']").TextContent.ShouldContain("fannst ekki");
        });
    }

    [Fact]
    public void ShowsErrorWithRetryButton_WhenHttpRequestFails()
    {
        // Arrange
        RegisterFailingHttpClient();

        // Act
        IRenderedComponent<TeamDetailsPage> cut = _context.Render<TeamDetailsPage>(
            p => p.Add(c => c.Slug, "thor"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find(".retry-btn").ShouldNotBeNull();
        });
    }

    [Fact]
    public void ShowsTeamTitle_WhenLoaded()
    {
        // Arrange
        TeamDetails team = new("thor", "Þór IF", "Þór", "Þór IF", "ISL", []);
        RegisterHttpClient(team);

        // Act
        IRenderedComponent<TeamDetailsPage> cut = _context.Render<TeamDetailsPage>(
            p => p.Add(c => c.Slug, "thor"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").TextContent.ShouldContain("Þór IF");
        });
    }

    [Fact]
    public void ShowsMemberList_WhenTeamHasMembers()
    {
        // Arrange
        TeamDetails team = new(
            "thor",
            "Þór IF",
            "Þór",
            "Þór IF",
            "ISL",
            [new TeamMember("jon-jonsson", "Jon Jonsson", 1990)]);
        RegisterHttpClient(team);

        // Act
        IRenderedComponent<TeamDetailsPage> cut = _context.Render<TeamDetailsPage>(
            p => p.Add(c => c.Slug, "thor"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find(".member-list").ShouldNotBeNull();
            cut.Find(".member-list").TextContent.ShouldContain("Jon Jonsson");
        });
    }

    [Fact]
    public void ShowsEmptyMembersMessage_WhenTeamHasNoMembers()
    {
        // Arrange
        TeamDetails team = new("thor", "Þór IF", "Þór", "Þór IF", "ISL", []);
        RegisterHttpClient(team);

        // Act
        IRenderedComponent<TeamDetailsPage> cut = _context.Render<TeamDetailsPage>(
            p => p.Add(c => c.Slug, "thor"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".member-list").Count.ShouldBe(0);
            cut.Find("p").TextContent.ShouldContain("Engir meðlimir");
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterHttpClient(TeamDetails team, bool delay = false)
    {
        TeamDetailsPageMockHandler handler = new(team, delay);
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterNullHttpClient()
    {
        NullTeamHttpMessageHandler handler = new();
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

    private sealed class TeamDetailsPageMockHandler(TeamDetails team, bool delay = false) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (delay)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(team),
            };
        }
    }

    private sealed class NullTeamHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class FailingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Server error");
        }
    }
}