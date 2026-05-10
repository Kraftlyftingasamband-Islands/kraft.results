using Bunit;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Web.Client.Features.Athletes;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Athletes;

public sealed class AthleteDetailsPageTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void RecordCard_ShowsSingleLiftBadge_WhenIsSingleLift()
    {
        // Arrange
        List<AthleteRecord> records =
        [
            new(
                Date: new DateOnly(2024, 3, 15),
                IsClassic: true,
                IsSingleLift: true,
                IsWithinPowerlifting: false,
                IsStandaloneDiscipline: false,
                WeightCategory: "83 kg",
                AgeCategory: "Open",
                Type: Constants.Bench,
                Weight: 150.0m,
                Meet: "Test Meet",
                MeetSlug: "test-meet"),
        ];

        RegisterHttpClient(records: records);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            List<string> badges = cut.FindAll(".arg-sl-badge")
                .Select(e => e.TextContent)
                .ToList();

            badges.ShouldContain(Constants.SingeLift);
        });
    }

    [Fact]
    public void RecordCard_DoesNotShowSingleLiftBadge_WhenIsNotSingleLift()
    {
        // Arrange
        List<AthleteRecord> records =
        [
            new(
                Date: new DateOnly(2024, 3, 15),
                IsClassic: true,
                IsSingleLift: false,
                IsWithinPowerlifting: false,
                IsStandaloneDiscipline: false,
                WeightCategory: "83 kg",
                AgeCategory: "Open",
                Type: Constants.Total,
                Weight: 600.0m,
                Meet: "Test Meet",
                MeetSlug: "test-meet"),
        ];

        RegisterHttpClient(records: records);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".arg-card").Count.ShouldBeGreaterThan(0);
            cut.FindAll(".arg-sl-badge").Count.ShouldBe(0);
        });
    }

    [Fact]
    public void PersonalBestGroupCard_ShowsRecordStar_WhenMatchingRecordExists()
    {
        // Arrange
        List<AthletePersonalBest> personalBests =
        [
            new(
                IsClassic: true,
                IsSingleLift: false,
                Discipline: Discipline.Squat,
                Weight: 200.0m,
                WeightCategory: "93 kg",
                BodyWeight: 90.5m,
                MeetSlug: "test-meet",
                MeetType: Constants.Powerlifting,
                Date: new DateOnly(2024, 5, 10)),
        ];

        List<AthleteRecord> records =
        [
            new(
                Date: new DateOnly(2024, 5, 10),
                IsClassic: true,
                IsSingleLift: false,
                IsWithinPowerlifting: false,
                IsStandaloneDiscipline: false,
                WeightCategory: "93 kg",
                AgeCategory: "open",
                Type: Constants.Squat,
                Weight: 200.0m,
                Meet: "Test Meet",
                MeetSlug: "test-meet"),
        ];

        RegisterHttpClient(records: records, personalBests: personalBests);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            List<string> stars = cut.FindAll(".pbg-star")
                .Select(e => e.GetAttribute("data-tooltip") ?? string.Empty)
                .ToList();

            stars.Count.ShouldBe(1);
            stars[0].ShouldBe("Íslandsmet · Opinn flokkur · 93 kg");
        });
    }

    [Fact]
    public void PersonalBestGroupCard_ShowsMultipleRecordStars_WhenMultipleAgeCategoriesMatch()
    {
        // Arrange
        List<AthletePersonalBest> personalBests =
        [
            new(
                IsClassic: true,
                IsSingleLift: false,
                Discipline: Discipline.None,
                Weight: 600.0m,
                WeightCategory: "93 kg",
                BodyWeight: 90.5m,
                MeetSlug: "test-meet",
                MeetType: Constants.Powerlifting,
                Date: new DateOnly(2024, 5, 10)),
        ];

        List<AthleteRecord> records =
        [
            new(
                Date: new DateOnly(2024, 5, 10),
                IsClassic: true,
                IsSingleLift: false,
                IsWithinPowerlifting: false,
                IsStandaloneDiscipline: false,
                WeightCategory: "93 kg",
                AgeCategory: "open",
                Type: Constants.Total,
                Weight: 600.0m,
                Meet: "Test Meet",
                MeetSlug: "test-meet"),
            new(
                Date: new DateOnly(2024, 5, 10),
                IsClassic: true,
                IsSingleLift: false,
                IsWithinPowerlifting: false,
                IsStandaloneDiscipline: false,
                WeightCategory: "93 kg",
                AgeCategory: "junior",
                Type: Constants.Total,
                Weight: 600.0m,
                Meet: "Test Meet",
                MeetSlug: "test-meet"),
        ];

        RegisterHttpClient(records: records, personalBests: personalBests);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            List<string> stars = cut.FindAll(".pbg-star")
                .Select(e => e.GetAttribute("data-tooltip") ?? string.Empty)
                .ToList();

            stars.Count.ShouldBe(2);
            stars.ShouldContain("Íslandsmet · Opinn flokkur · 93 kg");
            stars.ShouldContain("Íslandsmet · Unglingaflokkur · 93 kg");
        });
    }

    [Fact]
    public void PersonalBestGroupCard_ShowsTranslatedMastersCategory_InTooltip()
    {
        // Arrange
        List<AthletePersonalBest> personalBests =
        [
            new(
                IsClassic: true,
                IsSingleLift: false,
                Discipline: Discipline.Squat,
                Weight: 200.0m,
                WeightCategory: "93 kg",
                BodyWeight: 90.5m,
                MeetSlug: "test-meet",
                MeetType: Constants.Powerlifting,
                Date: new DateOnly(2024, 5, 10)),
        ];

        List<AthleteRecord> records =
        [
            new(
                Date: new DateOnly(2024, 5, 10),
                IsClassic: true,
                IsSingleLift: false,
                IsWithinPowerlifting: false,
                IsStandaloneDiscipline: false,
                WeightCategory: "93 kg",
                AgeCategory: "masters1",
                Type: Constants.Squat,
                Weight: 200.0m,
                Meet: "Test Meet",
                MeetSlug: "test-meet"),
        ];

        RegisterHttpClient(records: records, personalBests: personalBests);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            List<string> stars = cut.FindAll(".pbg-star")
                .Select(e => e.GetAttribute("data-tooltip") ?? string.Empty)
                .ToList();

            stars.Count.ShouldBe(1);
            stars[0].ShouldBe("Íslandsmet · Öldungaflokkur 1 · 93 kg");
        });
    }

    [Fact]
    public void PersonalBestGroupCard_DoesNotShowRecordStar_WhenNoMatchingRecord()
    {
        // Arrange
        List<AthletePersonalBest> personalBests =
        [
            new(
                IsClassic: true,
                IsSingleLift: false,
                Discipline: Discipline.Squat,
                Weight: 200.0m,
                WeightCategory: "93 kg",
                BodyWeight: 90.5m,
                MeetSlug: "test-meet",
                MeetType: Constants.Powerlifting,
                Date: new DateOnly(2024, 5, 10)),
        ];

        List<AthleteRecord> records =
        [
            new(
                Date: new DateOnly(2024, 5, 10),
                IsClassic: true,
                IsSingleLift: false,
                IsWithinPowerlifting: false,
                IsStandaloneDiscipline: false,
                WeightCategory: "93 kg",
                AgeCategory: "Open",
                Type: Constants.Bench,
                Weight: 150.0m,
                Meet: "Test Meet",
                MeetSlug: "test-meet"),
        ];

        RegisterHttpClient(records: records, personalBests: personalBests);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".pbg-card").Count.ShouldBeGreaterThan(0);
            cut.FindAll(".pbg-star").Count.ShouldBe(0);
        });
    }

    [Fact]
    public void ShowsLoadingStateInitially()
    {
        // Arrange
        RegisterHttpClient(delay: true);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.Find("[role='status']").ShouldNotBeNull();
        cut.Find(".visually-hidden").TextContent.ShouldBe("Sæki keppanda...");
    }

    [Fact]
    public void ShowsNotFoundMessage_WhenAthleteReturnsNull()
    {
        // Arrange
        RegisterNullAthleteHttpClient();

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "nonexistent"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find("[role='alert']").TextContent.ShouldContain("keppanda");
            cut.Find("[role='alert']").TextContent.ShouldContain("fannst ekki");
        });
    }

    [Fact]
    public void ShowsErrorWithRetryButton_WhenHttpRequestFails()
    {
        // Arrange
        RegisterFailingHttpClient();

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find(".retry-btn").ShouldNotBeNull();
        });
    }

    [Fact]
    public void ShowsAthleteName_WhenLoaded()
    {
        // Arrange
        RegisterHttpClient();

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("h1").TextContent.ShouldBe("Test Athlete");
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterHttpClient(
        List<AthleteRecord>? records = null,
        List<AthletePersonalBest>? personalBests = null,
        List<AthleteParticipation>? participations = null,
        bool delay = false)
    {
        AthleteDetailsPageMockHandler handler = new(
            records ?? [],
            personalBests ?? [],
            participations ?? [],
            delay);

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
        FailingAthleteHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
    }

    private sealed class NullAthleteHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }

    private sealed class FailingAthleteHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new HttpRequestException("Server error");
        }
    }
}