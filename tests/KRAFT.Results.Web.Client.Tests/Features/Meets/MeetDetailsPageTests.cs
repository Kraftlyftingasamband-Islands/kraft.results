using System.Net;
using System.Net.Http.Json;

using Bunit;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Web.Client.Features.Meets;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Meets;

public sealed class MeetDetailsPageTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsLoadingSpinner_WhenDataIsBeingFetched()
    {
        // Arrange
        RegisterHttpClient(delay: true);

        // Act
        IRenderedComponent<MeetDetailsPage> cut = _context.Render<MeetDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-meet"));

        // Assert
        cut.Find("[role='status']").ShouldNotBeNull();
        cut.Find(".visually-hidden").TextContent.ShouldBe("Sæki mót...");
    }

    [Fact]
    public void ShowsNotFoundMessage_WhenMeetReturnsNull()
    {
        // Arrange
        RegisterNullMeetHttpClient();

        // Act
        IRenderedComponent<MeetDetailsPage> cut = _context.Render<MeetDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "nonexistent"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find("[role='alert']").TextContent.ShouldContain("Mót");
            cut.Find("[role='alert']").TextContent.ShouldContain("fannst ekki");
        });
    }

    [Fact]
    public void ShowsErrorWithRetryButton_WhenHttpRequestFails()
    {
        // Arrange
        RegisterFailingHttpClient();

        // Act
        IRenderedComponent<MeetDetailsPage> cut = _context.Render<MeetDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-meet"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find(".retry-btn").ShouldNotBeNull();
        });
    }

    [Fact]
    public void ShowsMeetTitle_WhenLoaded()
    {
        // Arrange
        RegisterHttpClient();

        // Act
        IRenderedComponent<MeetDetailsPage> cut = _context.Render<MeetDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-meet"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").TextContent.ShouldBe("Test Meet");
        });
    }

    [Fact]
    public void DoesNotShowIpfPointLeaders_WhenNoIpfPointsPresent()
    {
        // Arrange
        List<MeetParticipation> participations = [MakeParticipation(ipfPoints: 0m)];
        RegisterHttpClient(participations: participations, calculatePlaces: true);

        // Act
        IRenderedComponent<MeetDetailsPage> cut = _context.Render<MeetDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-meet"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("#stigahaestu").Count.ShouldBe(0);
        });
    }

    [Fact]
    public void ShowsIpfPointLeaders_WhenCalculatePlacesAndIpfPointsPresent()
    {
        // Arrange
        List<MeetParticipation> participations = [MakeParticipation(ipfPoints: 450.5m)];
        RegisterHttpClient(participations: participations, calculatePlaces: true);

        // Act
        IRenderedComponent<MeetDetailsPage> cut = _context.Render<MeetDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-meet"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("#stigahaestu").ShouldNotBeNull();
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static MeetParticipation MakeParticipation(decimal ipfPoints = 0m) =>
        new(
            ParticipationId: 1,
            MeetId: 1,
            Rank: 1,
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            Gender: "M",
            YearOfBirth: 1990,
            AgeCategory: "Open",
            AgeCategorySlug: "open",
            WeightCategory: "83",
            Club: string.Empty,
            ClubSlug: string.Empty,
            BodyWeight: 82.5m,
            Total: 600m,
            IpfPoints: ipfPoints,
            Disqualified: false,
            Attempts: []);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterHttpClient(
        List<MeetParticipation>? participations = null,
        bool calculatePlaces = false,
        bool delay = false)
    {
        MeetDetailsPageMockHandler handler = new(
            participations ?? [],
            calculatePlaces,
            delay);

        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterNullMeetHttpClient()
    {
        NullMeetHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterFailingHttpClient()
    {
        FailingMeetHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private sealed class NullMeetHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class FailingMeetHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Server error");
        }
    }
}