namespace KRAFT.Results.WebApi.Features.Meets;

internal sealed class MeetType
{
    public int MeetTypeId { get; private set; }

    public string Title { get; private set; } = null!;

    public ICollection<Meet> Meets { get; } = [];
}