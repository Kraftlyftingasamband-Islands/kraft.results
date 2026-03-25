using KRAFT.Results.WebApi.Features.Athletes.Create;
using KRAFT.Results.WebApi.Features.Athletes.Get;
using KRAFT.Results.WebApi.Features.Athletes.GetDetails;
using KRAFT.Results.WebApi.Features.Athletes.GetEditDetails;
using KRAFT.Results.WebApi.Features.Athletes.GetParticipations;
using KRAFT.Results.WebApi.Features.Athletes.GetPersonalBests;
using KRAFT.Results.WebApi.Features.Athletes.GetRecords;
using KRAFT.Results.WebApi.Features.Athletes.Update;

namespace KRAFT.Results.WebApi.Features.Athletes;

internal static class AthleteEndpoints
{
    internal static IEndpointRouteBuilder MapAthleteEndpoints(this IEndpointRouteBuilder endpoints)
    {
        RouteGroupBuilder group = endpoints.MapGroup("/athletes")
            .WithTags("Athletes");

        group.MapCreateAthleteEndpoint();
        group.MapGetAthletesEndpoint();
        group.MapGetAthleteDetailsEndpoint();
        group.MapGetAthleteEditDetailsEndpoint();
        group.MapGetAthletePersonalBestsEndpoint();
        group.MapGetAthleteRecordsEndpoint();
        group.MapGetAthleteParticipationsEndpoint();
        group.MapUpdateAthleteEndpoint();

        return group;
    }
}