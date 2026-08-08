using Bunit;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Athletes;
using KRAFT.Results.Web.Client.Features.Athletes;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Athletes;

public sealed class AthleteDetailsPageTests : IDisposable
{
    private const string ImageBaseUrl = "https://example.blob.core.windows.net/images";

    private readonly BunitContext _context = new();

    [Fact]
    public void RecordCard_ShowsSingleLiftPill_WhenIsSingleLift()
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
            List<string> slPills = cut.FindAll(".esc-pill--single-lift")
                .Select(e => e.TextContent.Trim())
                .ToList();

            slPills.ShouldContain(Constants.SingeLift);
        });
    }

    [Fact]
    public void RecordCard_DoesNotShowSingleLiftPill_WhenIsNotSingleLift()
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
            cut.FindAll("span.esc-leading--badge").Count.ShouldBeGreaterThan(0);
            cut.FindAll(".esc-pill--single-lift").Count.ShouldBe(0);
        });
    }

    [Fact]
    public void WhenPbHoldsOneRecord_ShowsRecordPill()
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
                Date: new DateOnly(2024, 5, 10),
                Matches:
                [
                    new AthleteRecordMatch("Opinn flokkur", "93 kg", false, false, false),
                ]),
        ];

        RegisterHttpClient(personalBests: personalBests);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find(".esc-pill--record").TextContent.Trim().ShouldBe("★ Íslandsmet");
        });
    }

    [Fact]
    public void WhenPbHoldsMultipleRecords_ShowsCountSuffix()
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
                Date: new DateOnly(2024, 5, 10),
                Matches:
                [
                    new AthleteRecordMatch("Opinn flokkur", "93 kg", false, false, false),
                    new AthleteRecordMatch("Unglingaflokkur", "93 kg", false, false, false),
                ]),
        ];

        RegisterHttpClient(personalBests: personalBests);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find(".esc-pill--record").TextContent.Trim().ShouldBe("★ Íslandsmet ×2");
        });
    }

    [Fact]
    public void WhenPbHoldsMultipleRecords_TooltipShowsMultipleLines()
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
                Date: new DateOnly(2024, 5, 10),
                Matches:
                [
                    new AthleteRecordMatch("Opinn flokkur", "93 kg", false, false, false),
                    new AthleteRecordMatch("Unglingaflokkur", "93 kg", false, false, false),
                ]),
        ];

        RegisterHttpClient(personalBests: personalBests);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            string tooltip = cut.Find(".esc-pill--record").GetAttribute("data-tooltip").ShouldNotBeNull();
            tooltip.ShouldBe("Opinn flokkur · 93 kg\nUnglingaflokkur · 93 kg");
        });
    }

    [Fact]
    public void WhenPbHoldsNoRecord_ShowsNoRecordPill()
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
                Date: new DateOnly(2024, 5, 10),
                Matches: []),
        ];

        RegisterHttpClient(personalBests: personalBests);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("div.esc").Count.ShouldBeGreaterThan(0);
            cut.FindAll(".esc-pill--record").Count.ShouldBe(0);
        });
    }

    [Fact]
    public void WhenPbHoldsMastersRecord_TooltipRendersLabelVerbatim()
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
                Date: new DateOnly(2024, 5, 10),
                Matches:
                [
                    new AthleteRecordMatch("Öldungaflokkur 1", "93 kg", true, false, false),
                ]),
        ];

        RegisterHttpClient(personalBests: personalBests);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            string tooltip = cut.Find(".esc-pill--record").GetAttribute("data-tooltip").ShouldNotBeNull();
            tooltip.ShouldBe("Öldungaflokkur 1 · 93 kg (kraftlyftingar)");
        });
    }

    [Fact]
    public void WhenClassicAndEquipmentPbsBothExist_RendersClassicEraFirst()
    {
        // Arrange
        List<AthletePersonalBest> personalBests =
        [
            new(
                IsClassic: false,
                IsSingleLift: false,
                Discipline: Discipline.Squat,
                Weight: 150.0m,
                WeightCategory: "83 kg",
                BodyWeight: 80.0m,
                MeetSlug: "equipped-meet",
                MeetType: Constants.Powerlifting,
                Date: new DateOnly(2023, 1, 1),
                Matches: []),
            new(
                IsClassic: true,
                IsSingleLift: false,
                Discipline: Discipline.Squat,
                Weight: 200.0m,
                WeightCategory: "83 kg",
                BodyWeight: 80.0m,
                MeetSlug: "classic-meet",
                MeetType: Constants.Powerlifting,
                Date: new DateOnly(2024, 5, 10),
                Matches: []),
        ];

        RegisterHttpClient(personalBests: personalBests);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            List<AngleSharp.Dom.IElement> headings = cut.FindAll("h4")
                .Where(h => h.TextContent.Contains("Besti árangur", StringComparison.Ordinal))
                .ToList();

            headings.Count.ShouldBe(2);
            string classicEquipment = DisplayNames.EquipmentType(isClassic: true).ToLowerInvariant();
            string equippedEquipment = DisplayNames.EquipmentType(isClassic: false).ToLowerInvariant();
            headings[0].TextContent.Trim().ShouldBe($"Besti árangur {classicEquipment}");
            headings[1].TextContent.Trim().ShouldBe($"Besti árangur {equippedEquipment}");
        });
    }

    [Fact]
    public void WhenPbExists_RendersUnderCorrectEquipmentEraHeading()
    {
        // Arrange
        List<AthletePersonalBest> personalBests =
        [
            new(
                IsClassic: true,
                IsSingleLift: false,
                Discipline: Discipline.Squat,
                Weight: 200.0m,
                WeightCategory: "83 kg",
                BodyWeight: 80.0m,
                MeetSlug: "classic-meet",
                MeetType: Constants.Powerlifting,
                Date: new DateOnly(2024, 5, 10),
                Matches: []),
        ];

        RegisterHttpClient(personalBests: personalBests);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            string expectedEquipment = DisplayNames.EquipmentType(isClassic: true).ToLowerInvariant();
            AngleSharp.Dom.IElement heading = cut.FindAll("h4")
                .Single(h => h.TextContent.Contains("Besti árangur", StringComparison.Ordinal));
            heading.TextContent.Trim().ShouldBe($"Besti árangur {expectedEquipment}");
        });
    }

    [Fact]
    public void WhenPbHoldsOneRecord_EndToEndShowsRecordPillOnPage()
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
                Date: new DateOnly(2024, 5, 10),
                Matches:
                [
                    new AthleteRecordMatch("Opinn flokkur", "93 kg", true, false, false),
                ]),
        ];

        RegisterHttpClient(personalBests: personalBests);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            AngleSharp.Dom.IElement pill = cut.Find(".esc-pill--record");
            pill.TextContent.Trim().ShouldBe("★ Íslandsmet");
            string tooltip = pill.GetAttribute("data-tooltip").ShouldNotBeNull();
            tooltip.ShouldBe("Opinn flokkur · 93 kg (kraftlyftingar)");
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

    [Fact]
    public void RendersPhotoImg_InAthleteDetailHeader_WhenAthleteHasProfileImage()
    {
        // Arrange
        AthleteDetails athlete = AthleteDetailsPageMockHandler.AthleteWithPhoto("jondoe.jpg");
        RegisterHttpClient(athlete: athlete);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            AngleSharp.Dom.IElement img = cut.Find(".athlete-photo img");
            img.GetAttribute("src").ShouldStartWith($"{ImageBaseUrl}/jondoe.jpg");
        });
    }

    [Fact]
    public void RendersSvgPlaceholder_InAthleteDetailHeader_WhenAthleteHasNoProfileImage()
    {
        // Arrange
        RegisterHttpClient();

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find(".athlete-photo svg").ShouldNotBeNull();
            cut.FindAll(".athlete-photo img").Count.ShouldBe(0);
        });
    }

    [Fact]
    public void WhenAthleteHasClubWithLogo_RendersClubLogoImgWithPinnedVariantInsideNavLink()
    {
        // Arrange
        AthleteDetails athlete = AthleteDetailsPageMockHandler.AthleteWithClubLogo("breidablik.png");
        RegisterHttpClient(athlete: athlete);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            AngleSharp.Dom.IElement anchor = cut.Find(".club-anchor");
            anchor.GetAttribute("href").ShouldBe("/teams/test-club");
            anchor.GetAttribute("title").ShouldBe("Test Club");

            AngleSharp.Dom.IElement img = anchor.QuerySelector("img").ShouldNotBeNull();
            string src = img.GetAttribute("src").ShouldNotBeNull();
            src.ShouldStartWith($"{ImageBaseUrl}/breidablik.png");
            src.ShouldContain("width=128");
            src.ShouldContain("height=128");
        });
    }

    [Fact]
    public void WhenAthleteHasClubWithNoLogo_RendersMonogramInsideNavLink()
    {
        // Arrange
        AthleteDetails athlete = AthleteDetailsPageMockHandler.AthleteWithClubNoLogo();
        RegisterHttpClient(athlete: athlete);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            AngleSharp.Dom.IElement anchor = cut.Find(".club-anchor");
            anchor.GetAttribute("href").ShouldBe("/teams/test-club");
            anchor.GetAttribute("aria-label").ShouldBe("Test Club");

            AngleSharp.Dom.IElement fallback = anchor.QuerySelector(".team-logo-fallback-text").ShouldNotBeNull();
            fallback.TextContent.Trim().ShouldBe("KSV");

            anchor.QuerySelector("img").ShouldBeNull();
        });
    }

    [Fact]
    public void WhenAthleteHasClubButNoClubSlug_ClubAnchorIsAbsent_AndMetaRowShowsClub()
    {
        // Arrange
        AthleteDetails athlete = AthleteDetailsPageMockHandler.AthleteWithClubButNoSlug();
        RegisterHttpClient(athlete: athlete);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".club-anchor").Count.ShouldBe(0);
            cut.Find(".athlete-meta").TextContent.ShouldContain("Test Club");
        });
    }

    [Fact]
    public void WhenAthleteHasNoClub_ClubAnchorIsAbsent()
    {
        // Arrange
        AthleteDetails athlete = AthleteDetailsPageMockHandler.AthleteWithNoClub();
        RegisterHttpClient(athlete: athlete);

        // Act
        IRenderedComponent<AthleteDetailsPage> cut = _context.Render<AthleteDetailsPage>(
            parameters => parameters.Add(p => p.Slug, "test-athlete"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".club-anchor").Count.ShouldBe(0);
        });
    }

    [Fact]
    public void WhenAthleteHasCurrentRecords_RendersPerEraRecordHeading()
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
                AgeCategory: "open",
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
            string headingText = cut.Find("h4").TextContent.Trim();
            string expectedEquipment = DisplayNames.EquipmentType(isClassic: true).ToLowerInvariant();
            headingText.ShouldBe($"Íslandsmet {expectedEquipment} (1)");
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
        bool delay = false,
        AthleteDetails? athlete = null)
    {
        AthleteDetailsPageMockHandler handler = new(
            records ?? [],
            personalBests ?? [],
            participations ?? [],
            delay,
            athlete);

        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        RegisterConfiguration();
        _context.AddAuthorization();
    }

    private void RegisterConfiguration()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ImageBaseUrl"] = ImageBaseUrl,
            })
            .Build();
        _context.Services.AddSingleton(configuration);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterNullAthleteHttpClient()
    {
        NullAthleteHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        RegisterConfiguration();
        _context.AddAuthorization();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterFailingHttpClient()
    {
        FailingAthleteHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        RegisterConfiguration();
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
