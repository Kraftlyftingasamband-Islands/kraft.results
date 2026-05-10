using System.Net;
using System.Net.Http.Json;

using Bunit;

using KRAFT.Results.Contracts.Records;
using KRAFT.Results.Web.Client.Features.Records;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Records;

public sealed class RecordHistoryPageTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsLoadingSpinner_WhenDataIsBeingFetched()
    {
        // Arrange
        RegisterHttpClient(MakeResponse(), delay: true);

        // Act
        IRenderedComponent<RecordHistoryPage> cut = _context.Render<RecordHistoryPage>(
            parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.Find("[role='status']").ShouldNotBeNull();
        cut.Find(".visually-hidden").TextContent.ShouldBe("Sæki sögu...");
    }

    [Fact]
    public void ShowsErrorWithRetryButton_WhenHttpRequestFails()
    {
        // Arrange
        RegisterFailingHttpClient();

        // Act
        IRenderedComponent<RecordHistoryPage> cut = _context.Render<RecordHistoryPage>(
            parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find(".retry-btn").ShouldNotBeNull();
        });
    }

    [Fact]
    public void ShowsRecordHistoryEntries_WhenLoaded()
    {
        // Arrange
        RecordHistoryResponse response = MakeResponse(entries:
        [
            new(new DateOnly(2024, 1, 15), "Jón Jónsson", "jon-jonsson", 200m, 83m, "Nationals 2024", "nationals-2024", true, false, null),
        ]);
        RegisterHttpClient(response);

        // Act
        IRenderedComponent<RecordHistoryPage> cut = _context.Render<RecordHistoryPage>(
            parameters => parameters.Add(p => p.Id, 1));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".rhc-list .rhc-card").Count.ShouldBe(1);
        });
    }

    [Fact]
    public void BreadcrumbRemainsVisible_WhileLoading()
    {
        // Arrange
        RegisterHttpClient(MakeResponse(), delay: true);

        // Act
        IRenderedComponent<RecordHistoryPage> cut = _context.Render<RecordHistoryPage>(
            parameters => parameters.Add(p => p.Id, 1));

        // Assert — breadcrumb nav is outside DataLoader and always renders
        cut.Find("nav.breadcrumb").ShouldNotBeNull();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static RecordHistoryResponse MakeResponse(IReadOnlyList<RecordHistoryEntry>? entries = null) =>
        new(
            Category: "Squat",
            WeightCategory: "83",
            AgeCategory: "Open",
            Gender: "M",
            EquipmentType: "Classic",
            Era: null,
            Entries: entries ?? []);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterHttpClient(RecordHistoryResponse response, bool delay = false)
    {
        RecordHistoryMockHandler handler = new(response, delay);
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterFailingHttpClient()
    {
        FailingHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
        _context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private sealed class RecordHistoryMockHandler(RecordHistoryResponse response, bool delay = false) : HttpMessageHandler
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

    private sealed class FailingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Server error");
        }
    }
}