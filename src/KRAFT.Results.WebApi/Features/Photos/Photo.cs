using KRAFT.Results.WebApi.Features.Meets;

namespace KRAFT.Results.WebApi.Features.Photos;

internal sealed class Photo
{
    public int PhotoId { get; private set; }

    public int? MeetId { get; private set; }

    public string? Photographer { get; private set; }

    public DateTime Date { get; private set; }

    public string? ImageFilname { get; private set; }

    public DateTime CreatedOn { get; private set; }

    public string CreatedBy { get; private set; } = null!;

    public DateTime ModifiedOn { get; private set; }

    public string ModifiedBy { get; private set; } = null!;

    public Meet? Meet { get; private set; }
}