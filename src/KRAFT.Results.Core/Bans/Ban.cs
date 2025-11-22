namespace KRAFT.Results.Core.Bans;

internal sealed class Ban
{
    public int BanId { get; set; }

    public int AthleteId { get; set; }

    public DateTime FromDate { get; set; }

    public DateTime ToDate { get; set; }

    public DateTime CreatedOn { get; set; }
}