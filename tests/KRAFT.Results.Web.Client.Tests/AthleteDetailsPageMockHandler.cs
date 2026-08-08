using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Athletes;

namespace KRAFT.Results.Web.Client.Tests;

internal sealed class AthleteDetailsPageMockHandler(
    List<AthleteRecord> records,
    List<AthletePersonalBest> personalBests,
    List<AthleteParticipation> participations,
    bool delay = false,
    AthleteDetails? athlete = null) : HttpMessageHandler
{
    private static readonly AthleteDetails DefaultAthlete = new(
        Slug: "test-athlete",
        Name: "Test Athlete",
        YearOfBirth: 1990,
        Club: "Test Club",
        ClubSlug: "test-club",
        ClubShortTitle: "TCL",
        ClubLogoImageFilename: "default-club-logo.png",
        RecordCount: 0,
        ProfileImageFilename: null);

    private AthleteDetails ResolvedAthlete => athlete ?? DefaultAthlete;

    internal static AthleteDetails AthleteWithPhoto(string filename) =>
        DefaultAthlete with { ProfileImageFilename = filename };

    internal static AthleteDetails AthleteWithClubLogo(string logoFilename) =>
        DefaultAthlete with { ClubLogoImageFilename = logoFilename };

    internal static AthleteDetails AthleteWithClubNoLogo() =>
        DefaultAthlete with { ClubLogoImageFilename = null, ClubShortTitle = "KSV" };

    internal static AthleteDetails AthleteWithClubButNoSlug() =>
        DefaultAthlete with { ClubSlug = null };

    internal static AthleteDetails AthleteWithNoClub() =>
        DefaultAthlete with
        {
            Club = null,
            ClubSlug = null,
            ClubShortTitle = null,
            ClubLogoImageFilename = null,
        };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (delay)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        string path = request.RequestUri?.AbsolutePath ?? string.Empty;

        HttpResponseMessage response;

        if (path.EndsWith("/records", StringComparison.OrdinalIgnoreCase))
        {
            response = new(HttpStatusCode.OK) { Content = JsonContent.Create(records) };
        }
        else if (path.EndsWith("/personalbests", StringComparison.OrdinalIgnoreCase))
        {
            response = new(HttpStatusCode.OK) { Content = JsonContent.Create(personalBests) };
        }
        else if (path.EndsWith("/participations", StringComparison.OrdinalIgnoreCase))
        {
            response = new(HttpStatusCode.OK) { Content = JsonContent.Create(participations) };
        }
        else
        {
            response = new(HttpStatusCode.OK) { Content = JsonContent.Create(ResolvedAthlete) };
        }

        return response;
    }
}
