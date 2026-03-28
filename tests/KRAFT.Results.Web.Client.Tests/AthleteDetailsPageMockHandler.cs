using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Athletes;

namespace KRAFT.Results.Web.Client.Tests;

internal sealed class AthleteDetailsPageMockHandler(
    List<AthleteRecord> records,
    List<AthletePersonalBest> personalBests,
    List<AthleteParticipation> participations) : HttpMessageHandler
{
    private static readonly AthleteDetails DefaultAthlete = new(
        Slug: "test-athlete",
        Name: "Test Athlete",
        YearOfBirth: 1990,
        Club: "Test Club",
        ClubSlug: "test-club",
        RecordCount: 0);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
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
            response = new(HttpStatusCode.OK) { Content = JsonContent.Create(DefaultAthlete) };
        }

        return Task.FromResult(response);
    }
}