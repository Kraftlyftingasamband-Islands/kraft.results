using System.Net;
using System.Net.Http.Json;

using Bunit;

using KRAFT.Results.Contracts.Countries;
using KRAFT.Results.Web.Client.Features.Teams;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Teams;

public sealed class CreateTeamPageTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsLoadingSpinner_WhenDataIsBeingFetched()
    {
        // Arrange
        RegisterHttpClient(delay: true);

        // Act
        IRenderedComponent<CreateTeamPage> cut = _context.Render<CreateTeamPage>();

        // Assert
        cut.Find("[role='status']").ShouldNotBeNull();
        cut.Find(".visually-hidden").TextContent.ShouldBe("Sæki gögn...");
    }

    [Fact]
    public void ShowsErrorWithRetryButton_WhenHttpRequestFails()
    {
        // Arrange
        RegisterFailingHttpClient();

        // Act
        IRenderedComponent<CreateTeamPage> cut = _context.Render<CreateTeamPage>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find(".retry-btn").ShouldNotBeNull();
        });
    }

    [Fact]
    public void ShowsTeamForm_WhenLoaded()
    {
        // Arrange
        RegisterHttpClient();

        // Act
        IRenderedComponent<CreateTeamPage> cut = _context.Render<CreateTeamPage>();

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
        CreateTeamPageMockHandler handler = new(delay);
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

    private sealed class CreateTeamPageMockHandler(bool delay = false) : HttpMessageHandler
    {
        private static readonly List<CountrySummary> DefaultCountries =
        [
            new("ISL", "Iceland"),
        ];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (delay)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(DefaultCountries),
            };
        }
    }
}