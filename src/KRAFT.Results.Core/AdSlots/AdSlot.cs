namespace KRAFT.Results.Core.AdSlots;

internal sealed class AdSlot
{
    public string Id { get; set; } = null!;

    public string Description { get; set; } = null!;

    public bool Enabled { get; set; }

    public int Height { get; set; }

    public int Width { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime ModifiedOn { get; set; }
}