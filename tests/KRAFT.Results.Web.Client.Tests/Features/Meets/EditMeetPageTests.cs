using System.Net;
using System.Net.Http.Json;

using Bunit;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Web.Client.Features.Meets;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Meets;

public sealed class EditMeetPageTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsLoadingSpinner_WhenDataIsBeingFetched()
    {
        // Arrange
        RegisterHttpClient(delay: true);

        // Act
        IRenderedComponent<EditMeetPage> cut = _context.Render<EditMeetPage>(
            p => p.Add(c => c.Slug, "test-meet"));

        // Assert
        cut.Find("[role='status']").ShouldNotBeNull();
        cut.Find(".visually-hidden").TextContent.ShouldBe("Sæki gögn...");
    }

    [Fact]
    public void ShowsNotFoundMessage_WhenMeetReturnsNull()
    {
        // Arrange
        RegisterNullMeetHttpClient();

        // Act
        IRenderedComponent<EditMeetPage> cut = _context.Render<EditMeetPage>(
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
        IRenderedComponent<EditMeetPage> cut = _context.Render<EditMeetPage>(
            p => p.Add(c => c.Slug, "test-meet"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find(".retry-btn").ShouldNotBeNull();
        });
    }

    [Fact]
    public void ShowsMeetForm_WhenLoaded()
    {
        // Arrange
        RegisterHttpClient();

        // Act
        IRenderedComponent<EditMeetPage> cut = _context.Render<EditMeetPage>(
            p => p.Add(c => c.Slug, "test-meet"));

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
        EditMeetPageMockHandler handler = new(delay);
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterNullMeetHttpClient()
    {
        NullMeetHttpMessageHandler handler = new();
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

    private sealed class EditMeetPageMockHandler(bool delay = false) : HttpMessageHandler
    {
        private readonly MeetDetails _meet = new(
            MeetId: 1,
            Title: "Test Meet",
            Slug: "test-meet",
            Location: "Reykjavik",
            Text: string.Empty,
            StartDate: new DateOnly(2024, 1, 1),
            EndDate: null,
            Type: "Powerlifting",
            MeetTypeId: 1,
            ResultMode: "Open",
            CalculatePlaces: true,
            IsInTeamCompetition: false,
            ShowWilks: false,
            ShowTeams: false,
            ShowBodyWeight: false,
            PublishedInCalendar: false,
            PublishedResults: false,
            RecordsPossible: false,
            IsClassic: false,
            ShowTeamPoints: false,
            Disciplines: []);

        private readonly List<MeetTypeSummary> _meetTypes =
        [
            new(1, "Powerlifting", "Styrktarþraut", ["Squat", "Bench", "Deadlift"]),
        ];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (delay)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            string path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.StartsWith("/meets/types", StringComparison.OrdinalIgnoreCase))
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_meetTypes),
                };
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(_meet),
            };
        }
    }

    private sealed class NullMeetHttpMessageHandler : HttpMessageHandler
    {
        private readonly List<MeetTypeSummary> _meetTypes =
        [
            new(1, "Powerlifting", "Styrktarþraut", ["Squat", "Bench", "Deadlift"]),
        ];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string path = request.RequestUri?.AbsolutePath ?? string.Empty;

            if (path.StartsWith("/meets/types", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(_meetTypes),
                });
            }

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