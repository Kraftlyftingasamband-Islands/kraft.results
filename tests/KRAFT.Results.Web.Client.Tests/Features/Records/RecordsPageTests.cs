using System.Net;
using System.Net.Http.Json;

using Bunit;

using KRAFT.Results.Contracts.Eras;
using KRAFT.Results.Contracts.Records;
using KRAFT.Results.Web.Client.Features.Records;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Records;

public sealed class RecordsPageTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsLoadingSpinner_WhenRecordsAreBeingFetched()
    {
        // Arrange
        RegisterHttpClient(eras: MakeEras(), groups: MakeGroups(), delayRecords: true);

        // Act
        IRenderedComponent<RecordsPage> cut = _context.Render<RecordsPage>(
            parameters => parameters
                .Add(p => p.Gender, "m")
                .Add(p => p.AgeCategory, "open"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='status']").ShouldNotBeNull();
            cut.Find(".visually-hidden").TextContent.ShouldBe("Sæki met...");
        });
    }

    [Fact]
    public void ShowsErrorWithRetryButton_WhenRecordsFetchFails()
    {
        // Arrange
        RegisterHttpClient(eras: MakeEras(), groups: null, failRecords: true);

        // Act
        IRenderedComponent<RecordsPage> cut = _context.Render<RecordsPage>(
            parameters => parameters
                .Add(p => p.Gender, "m")
                .Add(p => p.AgeCategory, "open"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find(".retry-btn").ShouldNotBeNull();
        });
    }

    [Fact]
    public void ShowsRecordSections_WhenGroupsAreLoaded()
    {
        // Arrange
        List<RecordGroup> groups =
        [
            new("Squat", [new(1, "83", "Jón Jónsson", "jon-jonsson", null, null, 200m, new DateOnly(2024, 1, 1), null, true)]),
        ];
        RegisterHttpClient(eras: MakeEras(), groups: groups);

        // Act
        IRenderedComponent<RecordsPage> cut = _context.Render<RecordsPage>(
            parameters => parameters
                .Add(p => p.Gender, "m")
                .Add(p => p.AgeCategory, "open"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".record-section").Count.ShouldBe(1);
        });
    }

    [Fact]
    public void ToolbarRemainsVisible_WhileRecordsAreLoading()
    {
        // Arrange
        RegisterHttpClient(eras: MakeEras(), groups: MakeGroups(), delayRecords: true);

        // Act
        IRenderedComponent<RecordsPage> cut = _context.Render<RecordsPage>(
            parameters => parameters
                .Add(p => p.Gender, "m")
                .Add(p => p.AgeCategory, "open"));

        // Assert — toolbar is outside DataLoader and always renders after eras load
        cut.WaitForAssertion(() =>
        {
            cut.Find(".records-toolbar").ShouldNotBeNull();
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    private static List<EraSummary> MakeEras() =>
    [
        new(1, "IPF Era", "ipf-era", new DateOnly(2020, 1, 1), new DateOnly(2024, 12, 31), true, false),
    ];

    private static List<RecordGroup> MakeGroups() => [];

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterHttpClient(
        List<EraSummary> eras,
        List<RecordGroup>? groups,
        bool delayRecords = false,
        bool failRecords = false)
    {
        RecordsMockHandler handler = new(eras, groups, delayRecords, failRecords);
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
        _context.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    private sealed class RecordsMockHandler(
        List<EraSummary> eras,
        List<RecordGroup>? groups,
        bool delayRecords = false,
        bool failRecords = false) : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri?.PathAndQuery.StartsWith("/eras", StringComparison.Ordinal) == true)
            {
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(eras),
                };
            }

            if (failRecords)
            {
                throw new HttpRequestException("Server error");
            }

            if (delayRecords)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(groups),
            };
        }
    }
}