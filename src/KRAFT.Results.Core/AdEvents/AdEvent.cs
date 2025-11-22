namespace KRAFT.Results.Core.AdEvents;

internal class AdEvent
{
    public int AdEventId { get; set; }

    public int AdEventType { get; set; }

    public int AdId { get; set; }

    public string Ip { get; set; } = null!;

    public DateTime CreatedOn { get; set; }
}