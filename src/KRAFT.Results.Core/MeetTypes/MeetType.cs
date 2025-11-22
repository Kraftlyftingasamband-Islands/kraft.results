using KRAFT.Results.Core.Meets;

namespace KRAFT.Results.Core.MeetTypes;

internal sealed class MeetType
{
    public int MeetTypeId { get; set; }

    public string Title { get; set; } = null!;

    public ICollection<Meet> Meets { get; } = [];
}