using KRAFT.Results.Core.Meets;

namespace KRAFT.Results.Core.MeetTypes;

internal class MeetType
{
    public int MeetTypeId { get; set; }

    public string Title { get; set; } = null!;

    public virtual ICollection<Meet> Meets { get; } = [];
}