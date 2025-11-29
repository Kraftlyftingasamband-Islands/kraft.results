namespace KRAFT.Results.WebApi.Features.Wilks;

internal sealed class Wilk
{
    public decimal Weight { get; private set; }

    public decimal Coefficient { get; private set; }

    public string Gender { get; private set; } = null!;

    public int Id { get; private set; }
}