using KRAFT.Results.WebApi.Features.Meets;

namespace KRAFT.Results.WebApi.Features.MeetTypes;

internal sealed class MeetType
{
    public int MeetTypeId { get; set; }

    public string Title { get; set; } = null!;

    public ICollection<Meet> Meets { get; } = [];
}