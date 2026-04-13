using System.Net;

using Bunit;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;
using KRAFT.Results.Web.Client.Features.Meets;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Meets;

public sealed class ParticipationCardTests : IDisposable
{
    private readonly BunitContext _context = new();
    private readonly FakeHttpHandler _httpHandler = new();
    private readonly HttpClient _httpClient;

    public ParticipationCardTests()
    {
        _httpClient = new HttpClient(_httpHandler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(_httpClient);
        _context.AddAuthorization();
        _context.JSInterop.Mode = Bunit.JSRuntimeMode.Loose;
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

        // Assert — pill shows the new value, not the original 200.0
        string expected = 210m.ToString("F1", System.Globalization.CultureInfo.CurrentCulture);
        cut.Find(".pill").TextContent.Trim().ShouldBe(expected);
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
        string expected = 210m.ToString("F1", System.Globalization.CultureInfo.CurrentCulture);
        AngleSharp.Dom.IElement inputAgain = cut.Find("input.inline-attempt-input");
        inputAgain.GetAttribute("value").ShouldBe(expected);
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