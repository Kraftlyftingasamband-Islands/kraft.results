using KRAFT.Results.Core.Meets;

namespace KRAFT.Results.Core.Photos;

internal sealed class Photo
{
    public int PhotoId { get; set; }

    public int? MeetId { get; set; }

    public string? Photographer { get; set; }

    public DateTime Date { get; set; }

    public string? ImageFilname { get; set; }

    public DateTime CreatedOn { get; set; }

    public string CreatedBy { get; set; } = null!;

    public DateTime ModifiedOn { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public Meet? Meet { get; set; }
}