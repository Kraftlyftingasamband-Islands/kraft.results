using KRAFT.Results.WebApi.Features.EraWeightCategories;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records;
using KRAFT.Results.WebApi.ValueObjects;

namespace KRAFT.Results.WebApi.Features.WeightCategories;

internal sealed class WeightCategory
{
    public int WeightCategoryId { get; private set; }

    public string Title { get; private set; } = null!;

    public decimal MinWeight { get; private set; }

    public decimal MaxWeight { get; private set; }

    public Gender Gender { get; private set; } = null!;

    public DateTime CreatedOn { get; private set; }

    public bool JuniorsOnly { get; private set; }

    public string Slug { get; private set; } = null!;

    public ICollection<EraWeightCategory> EraWeightCategories { get; } = [];

    public ICollection<Participation> Participations { get; } = [];

    public ICollection<Record> Records { get; } = [];

    internal static WeightCategory? FindBestFit(IEnumerable<WeightCategory> eligible, decimal bodyWeight)
    {
        WeightCategory? best = null;

        foreach (WeightCategory category in eligible)
        {
            if (category.MaxWeight >= bodyWeight && (best is null || category.MaxWeight < best.MaxWeight))
            {
                best = category;
            }
        }

        return best;
    }
}