using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts;
using KRAFT.Results.Contracts.Meets;

namespace KRAFT.Results.Web.Client.Tests;

internal sealed class MeetDetailsPageMockHandler(
    List<MeetParticipation> participations,
    bool calculatePlaces = false,
    bool delay = false) : HttpMessageHandler
{
    private readonly MeetDetails _meet = new(
        MeetId: 1,
        Title: "Test Meet",
        Slug: "test-meet",
        Location: "Reykjavík",
        Text: string.Empty,
        StartDate: new DateOnly(2024, 3, 15),
        EndDate: null,
        Type: "Powerlifting",
        MeetTypeId: 1,
        ResultMode: "Standard",
        CalculatePlaces: calculatePlaces,
        IsInTeamCompetition: false,
        ShowWilks: false,
        ShowTeams: false,
        ShowBodyWeight: true,
        PublishedInCalendar: true,
        PublishedResults: true,
        RecordsPossible: false,
        IsClassic: true,
        ShowTeamPoints: false,
        Disciplines: [Discipline.Squat, Discipline.Bench, Discipline.Deadlift]);

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (delay)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        string path = request.RequestUri?.AbsolutePath ?? string.Empty;

        if (path.EndsWith("/participations", StringComparison.OrdinalIgnoreCase))
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(participations),
            };
        }

        if (path.EndsWith("/records", StringComparison.OrdinalIgnoreCase))
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new List<MeetRecordEntry>()),
            };
        }

        if (path.EndsWith("/team-points", StringComparison.OrdinalIgnoreCase))
        {
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create<MeetTeamPointsResponse?>(null),
            };
        }

        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(_meet),
        };
    }
}