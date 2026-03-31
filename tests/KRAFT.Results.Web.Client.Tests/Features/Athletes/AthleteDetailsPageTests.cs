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
            List<string> badges = cut.FindAll(".ar-sl-badge")
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
            cut.FindAll(".ar-card").Count.ShouldBeGreaterThan(0);
            cut.FindAll(".ar-sl-badge").Count.ShouldBe(0);
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
        List<AthleteParticipation>? participations = null)
    {
        AthleteDetailsPageMockHandler handler = new(
            records ?? [],
            personalBests ?? [],
            participations ?? []);

        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
    }
}