namespace KRAFT.Results.Core.Ads;

internal sealed class Ad
{
    public int AdId { get; set; }

    public string AdSlotId { get; set; } = null!;

    public string ImageUrl { get; set; } = null!;

    public string ClickUrl { get; set; } = null!;

    public bool Enabled { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string ModifiedBy { get; set; } = null!;
}