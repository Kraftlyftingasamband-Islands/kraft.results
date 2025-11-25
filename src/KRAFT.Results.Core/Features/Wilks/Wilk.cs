namespace KRAFT.Results.Core.Features.Wilks;

internal sealed class Wilk
{
    public decimal Weight { get; set; }

    public decimal Coefficient { get; set; }

    public string Gender { get; set; } = null!;

    public int Id { get; set; }
}