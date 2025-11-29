namespace KRAFT.Results.WebApi.Features.Meets;

internal sealed class MeetType
{
    public int MeetTypeId { get; set; }

    public string Title { get; set; } = null!;

    public ICollection<Meet> Meets { get; } = [];
}