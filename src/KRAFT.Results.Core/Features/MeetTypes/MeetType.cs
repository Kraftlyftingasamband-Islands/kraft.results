using KRAFT.Results.Core.Features.Meets;

namespace KRAFT.Results.Core.Features.MeetTypes;

internal sealed class MeetType
{
    public int MeetTypeId { get; set; }

    public string Title { get; set; } = null!;

    public ICollection<Meet> Meets { get; } = [];
}