using System.Net;
using System.Net.Http.Json;

using Bunit;

using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Contracts.Countries;
using KRAFT.Results.Contracts.Teams;
using KRAFT.Results.Web.Client.Features.Athletes;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Athletes;

public sealed class EditAthletePageTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsLoadingSpinner_WhenDataIsBeingFetched()
    {
        // Arrange
        RegisterHttpClient(delay: true);

        // Act
        IRenderedComponent<EditAthletePage> cut = _context.Render<EditAthletePage>(
            p => p.Add(c => c.Slug, "test-athlete"));

        // Assert
        cut.Find("[role='status']").ShouldNotBeNull();
        cut.Find(".visually-hidden").TextContent.ShouldBe("Sæki gögn...");
    }

    [Fact]
    public void ShowsNotFoundMessage_WhenAthleteReturnsNull()
    {
        // Arrange
        RegisterNullAthleteHttpClient();

        // Act
        IRenderedComponent<EditAthletePage> cut = _context.Render<EditAthletePage>(
            p => p.Add(c => c.Slug, "nonexistent"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find("[role='alert']").TextContent.ShouldContain("fannst ekki");
        });
    }

    [Fact]
    public void ShowsErrorWithRetryButton_WhenHttpRequestFails()
    {
        // Arrange
        RegisterFailingHttpClient();

        // Act
        IRenderedComponent<EditAthletePage> cut = _context.Render<EditAthletePage>(
            p => p.Add(c => c.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find(".retry-btn").ShouldNotBeNull();
        });
    }

    [Fact]
    public void ShowsAthleteForm_WhenLoaded()
    {
        // Arrange
        RegisterHttpClient();

        // Act
        IRenderedComponent<EditAthletePage> cut = _context.Render<EditAthletePage>(
            p => p.Add(c => c.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("form").ShouldNotBeNull();
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterHttpClient(bool delay = false)
    {
        EditAthletePageMockHandler handler = new(delay);
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterNullAthleteHttpClient()
    {
        NullAthleteHttpMessageHandler handler = new();
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

    private sealed class EditAthletePageMockHandler(bool delay = false) : HttpMessageHandler
    {
        private static readonly AthleteEditDetails DefaultAthlete = new(
            FirstName: "Jon",
            LastName: "Jonsson",
            DateOfBirth: new DateOnly(1990, 1, 1),
            Gender: "M",
            CountryCode: "ISL",
            TeamId: null);

        private static readonly List<CountrySummary> DefaultCountries =
        [
            new("ISL", "Iceland"),
        ];

        private static readonly List<TeamOption> DefaultTeams =
        [
            new(1, "Þór"),
        ];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (delay)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            string path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.StartsWith("/countries", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(DefaultCountries),
                };
            }

            if (path.StartsWith("/teams/options", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(DefaultTeams),
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(DefaultAthlete),
            };
        }
    }

    private sealed class NullAthleteHttpMessageHandler : HttpMessageHandler
    {
        private static readonly List<CountrySummary> DefaultCountries = [new("ISL", "Iceland")];
        private static readonly List<TeamOption> DefaultTeams = [new(1, "Þór")];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.StartsWith("/countries", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(DefaultCountries),
                });
            }

            if (path.StartsWith("/teams/options", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(DefaultTeams),
                });
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}