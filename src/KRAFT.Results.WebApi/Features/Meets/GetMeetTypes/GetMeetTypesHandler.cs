using KRAFT.Results.Contracts.Meets;

namespace KRAFT.Results.WebApi.Features.Meets.GetMeetTypes;

internal sealed class GetMeetTypesHandler
{
    private static readonly List<MeetTypeSummary> MeetTypes = Enum.GetValues<MeetCategory>()
        .Select(c => new MeetTypeSummary((int)c, c.ToString()))
        .ToList();

#pragma warning disable CA1822, S2325 // Kept as instance method for DI resolution
    public Task<List<MeetTypeSummary>> Handle(CancellationToken cancellationToken) =>
        Task.FromResult(MeetTypes);
#pragma warning restore CA1822, S2325
}