using System.Net;

using Bunit;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Web.Client.Features.Meets;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Meets;

public sealed class ParticipationCardTests : IDisposable
{
    private const string ImageBaseUrl = "https://example.blob.core.windows.net/images";

    private readonly BunitContext _context = new();
    private readonly FakeHttpHandler _httpHandler = new();
    private readonly HttpClient _httpClient;

    public ParticipationCardTests()
    {
        _httpClient = new HttpClient(_httpHandler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(_httpClient);
        _context.AddAuthorization();
        _context.JSInterop.Mode = Bunit.JSRuntimeMode.Loose;

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ImageBaseUrl"] = ImageBaseUrl,
            })
            .Build();
        _context.Services.AddSingleton(configuration);
    }

    [Fact]
    public async Task ExistingAttempt_WhenEdited_ShowsNewValueAfterSave()
    {
        // Arrange — participation with one existing squat attempt at 200 kg
        MeetParticipation participation = MakeParticipation(
            [new MeetAttempt(Discipline.Squat, 1, 200m, true, false)]);

        IRenderedComponent<ParticipationCard> cut = _context.Render<ParticipationCard>(
            p => p.Add(c => c.Participation, participation)
                  .Add(c => c.ShowIpfPoints, false)
                  .Add(c => c.ShowClub, false)
                  .Add(c => c.IncludedDisciplines, new Dictionary<Discipline, bool> { [Discipline.Squat] = true })
                  .Add(c => c.DesktopGridTemplate, "auto")
                  .Add(c => c.IsAdmin, true));

        // Act — click the pill to enter edit mode
        AngleSharp.Dom.IElement pillButton = cut.Find("button.pill-clickable");
        await cut.InvokeAsync(() => pillButton.Click());

        // Type a new value and blur to commit
        AngleSharp.Dom.IElement input = cut.Find("input.inline-attempt-input");
        await cut.InvokeAsync(() => input.Input("210"));
        await cut.InvokeAsync(() => input.Blur());

        // Assert — pill shows the new value, not the original 200,0
        cut.Find(".pill").TextContent.Trim().ShouldBe("210,0");
    }

    [Fact]
    public async Task ExistingAttempt_WhenReEdited_InputPreFillsWithSavedValue()
    {
        // Arrange — participation with one existing squat attempt at 200 kg
        MeetParticipation participation = MakeParticipation(
            [new MeetAttempt(Discipline.Squat, 1, 200m, true, false)]);

        IRenderedComponent<ParticipationCard> cut = _context.Render<ParticipationCard>(
            p => p.Add(c => c.Participation, participation)
                  .Add(c => c.ShowIpfPoints, false)
                  .Add(c => c.ShowClub, false)
                  .Add(c => c.IncludedDisciplines, new Dictionary<Discipline, bool> { [Discipline.Squat] = true })
                  .Add(c => c.DesktopGridTemplate, "auto")
                  .Add(c => c.IsAdmin, true));

        // First edit — change 200 → 210 and save
        AngleSharp.Dom.IElement pillButton = cut.Find("button.pill-clickable");
        await cut.InvokeAsync(() => pillButton.Click());

        AngleSharp.Dom.IElement input = cut.Find("input.inline-attempt-input");
        await cut.InvokeAsync(() => input.Input("210"));
        await cut.InvokeAsync(() => input.Blur());

        // Act — click the pill again to re-enter edit mode
        AngleSharp.Dom.IElement pillButtonAgain = cut.Find("button.pill-clickable");
        await cut.InvokeAsync(() => pillButtonAgain.Click());

        // Assert — input pre-fills with 210, not the original 200
        AngleSharp.Dom.IElement inputAgain = cut.Find("input.inline-attempt-input");
        inputAgain.GetAttribute("value").ShouldBe("210,0");
    }

    [Fact]
    public async Task ExistingAttempt_WhenFirstEdited_InputPreFillsWithCommaDecimal()
    {
        // Arrange — participation with one existing squat attempt at 147,5 kg
        MeetParticipation participation = MakeParticipation(
            [new MeetAttempt(Discipline.Squat, 1, 147.5m, true, false)]);

        IRenderedComponent<ParticipationCard> cut = _context.Render<ParticipationCard>(
            p => p.Add(c => c.Participation, participation)
                  .Add(c => c.ShowIpfPoints, false)
                  .Add(c => c.ShowClub, false)
                  .Add(c => c.IncludedDisciplines, new Dictionary<Discipline, bool> { [Discipline.Squat] = true })
                  .Add(c => c.DesktopGridTemplate, "auto")
                  .Add(c => c.IsAdmin, true));

        // Act — click the pill to enter edit mode for the first time
        AngleSharp.Dom.IElement pillButton = cut.Find("button.pill-clickable");
        await cut.InvokeAsync(() => pillButton.Click());

        // Assert — input pre-fills with Icelandic comma decimal, not a dot
        AngleSharp.Dom.IElement input = cut.Find("input.inline-attempt-input");
        input.GetAttribute("value").ShouldBe("147,5");
    }

    [Fact]
    public async Task WhenFailedAttemptIsEdited_InputPreFillsWithNegativeCommaForm()
    {
        // Arrange — participation with a failed squat attempt at 147.5 kg (IsGood = false)
        MeetParticipation participation = MakeParticipation(
            [new MeetAttempt(Discipline.Squat, 1, 147.5m, false, false)]);

        IRenderedComponent<ParticipationCard> cut = _context.Render<ParticipationCard>(
            p => p.Add(c => c.Participation, participation)
                  .Add(c => c.ShowIpfPoints, false)
                  .Add(c => c.ShowClub, false)
                  .Add(c => c.IncludedDisciplines, new Dictionary<Discipline, bool> { [Discipline.Squat] = true })
                  .Add(c => c.DesktopGridTemplate, "auto")
                  .Add(c => c.IsAdmin, true));

        // Act — click the pill to enter edit mode for the first time
        AngleSharp.Dom.IElement pillButton = cut.Find("button.pill-clickable");
        await cut.InvokeAsync(() => pillButton.Click());

        // Assert — input pre-fills with Icelandic negative comma decimal
        AngleSharp.Dom.IElement input = cut.Find("input.inline-attempt-input");
        input.GetAttribute("value").ShouldBe("-147,5");
    }

    [Fact]
    public async Task AttemptInput_WhenUserTypesDot_ParsesAndSavesCorrectly()
    {
        // Arrange — participation with no existing attempts
        MeetParticipation participation = MakeParticipation([]);

        IRenderedComponent<ParticipationCard> cut = _context.Render<ParticipationCard>(
            p => p.Add(c => c.Participation, participation)
                  .Add(c => c.ShowIpfPoints, false)
                  .Add(c => c.ShowClub, false)
                  .Add(c => c.IncludedDisciplines, new Dictionary<Discipline, bool> { [Discipline.Squat] = true })
                  .Add(c => c.DesktopGridTemplate, "auto")
                  .Add(c => c.IsAdmin, true));

        // Act — click an empty pill, type using dot decimal separator, then blur to save
        AngleSharp.Dom.IElement pillButton = cut.Find("button.pill-clickable");
        await cut.InvokeAsync(() => pillButton.Click());

        AngleSharp.Dom.IElement input = cut.Find("input.inline-attempt-input");
        await cut.InvokeAsync(() => input.Input("147.5"));
        await cut.InvokeAsync(() => input.Blur());

        // Assert — pill shows the comma form (no attempt-error visible)
        cut.FindAll(".attempt-error").Count.ShouldBe(0);
        cut.Find(".pill").TextContent.Trim().ShouldBe("147,5");
    }

    [Fact]
    public async Task AttemptInput_WhenUserTypesComma_ParsesAndSavesCorrectly()
    {
        // Arrange — participation with no existing attempts
        MeetParticipation participation = MakeParticipation([]);

        IRenderedComponent<ParticipationCard> cut = _context.Render<ParticipationCard>(
            p => p.Add(c => c.Participation, participation)
                  .Add(c => c.ShowIpfPoints, false)
                  .Add(c => c.ShowClub, false)
                  .Add(c => c.IncludedDisciplines, new Dictionary<Discipline, bool> { [Discipline.Squat] = true })
                  .Add(c => c.DesktopGridTemplate, "auto")
                  .Add(c => c.IsAdmin, true));

        // Act — click an empty pill, type using comma decimal separator, then blur to save
        AngleSharp.Dom.IElement pillButton = cut.Find("button.pill-clickable");
        await cut.InvokeAsync(() => pillButton.Click());

        AngleSharp.Dom.IElement input = cut.Find("input.inline-attempt-input");
        await cut.InvokeAsync(() => input.Input("147,5"));
        await cut.InvokeAsync(() => input.Blur());

        // Assert — pill shows the comma form (no attempt-error visible)
        cut.FindAll(".attempt-error").Count.ShouldBe(0);
        cut.Find(".pill").TextContent.Trim().ShouldBe("147,5");
    }

    [Fact]
    public void WhenShowClubTrue_AndClubHasLogo_RendersImgInsideNavLinkWithTitleAndAlt()
    {
        // Arrange
        MeetParticipation participation = MakeParticipationWithClub(
            club: "Þór",
            clubSlug: "thor",
            clubLogoImageFilename: "thor.png",
            attempts: []);

        // Act
        IRenderedComponent<ParticipationCard> cut = _context.Render<ParticipationCard>(
            p => p.Add(c => c.Participation, participation)
                  .Add(c => c.ShowIpfPoints, false)
                  .Add(c => c.ShowClub, true)
                  .Add(c => c.IncludedDisciplines, new Dictionary<Discipline, bool> { [Discipline.Squat] = true })
                  .Add(c => c.DesktopGridTemplate, "auto")
                  .Add(c => c.IsAdmin, false));

        // Assert
        AngleSharp.Dom.IElement navLink = cut.Find("a.p-club");
        navLink.GetAttribute("href").ShouldBe("/teams/thor");
        navLink.GetAttribute("title").ShouldBe("Þór");
        AngleSharp.Dom.IElement img = cut.Find(".p-club img");
        img.GetAttribute("alt").ShouldBe("Þór");
    }

    [Fact]
    public void WhenShowClubTrue_AndClubHasNoLogo_RendersClubNameText()
    {
        // Arrange
        MeetParticipation participation = MakeParticipationWithClub(
            club: "Þór",
            clubSlug: "thor",
            clubLogoImageFilename: null,
            attempts: []);

        // Act
        IRenderedComponent<ParticipationCard> cut = _context.Render<ParticipationCard>(
            p => p.Add(c => c.Participation, participation)
                  .Add(c => c.ShowIpfPoints, false)
                  .Add(c => c.ShowClub, true)
                  .Add(c => c.IncludedDisciplines, new Dictionary<Discipline, bool> { [Discipline.Squat] = true })
                  .Add(c => c.DesktopGridTemplate, "auto")
                  .Add(c => c.IsAdmin, false));

        // Assert
        AngleSharp.Dom.IElement navLink = cut.Find("a.p-club");
        navLink.GetAttribute("href").ShouldBe("/teams/thor");
        navLink.TextContent.Trim().ShouldContain("Þór");
        cut.FindAll(".p-club img").Count.ShouldBe(0);
    }

    public void Dispose()
    {
        _context.Dispose();
        _httpClient.Dispose();
        _httpHandler.Dispose();
    }

    private static MeetParticipation MakeParticipation(IEnumerable<MeetAttempt> attempts) =>
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
            ClubLogoImageFilename: null,
            BodyWeight: 82.5m,
            Total: 0m,
            IpfPoints: 0m,
            Disqualified: false,
            Attempts: attempts);

    private static MeetParticipation MakeParticipationWithClub(
        string club,
        string clubSlug,
        string? clubLogoImageFilename,
        IEnumerable<MeetAttempt> attempts) =>
        new(
            ParticipationId: 2,
            MeetId: 1,
            Rank: 1,
            Athlete: "Jón Jónsson",
            AthleteSlug: "jon-jonsson",
            Gender: "M",
            YearOfBirth: 1990,
            AgeCategory: "Open",
            AgeCategorySlug: "open",
            WeightCategory: "83",
            Club: club,
            ClubSlug: clubSlug,
            ClubLogoImageFilename: clubLogoImageFilename,
            BodyWeight: 82.5m,
            Total: 0m,
            IpfPoints: 0m,
            Disqualified: false,
            Attempts: attempts);

    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // PUT (save attempt) succeeds; GET (refresh) fails so RefreshParticipation is silently ignored
            HttpStatusCode status = request.Method == HttpMethod.Get
                ? HttpStatusCode.ServiceUnavailable
                : HttpStatusCode.OK;
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }
}
