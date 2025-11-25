using KRAFT.Results.WebApi.Features.EraWeightCategories;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records;

namespace KRAFT.Results.WebApi.Features.WeightCategories;

internal sealed class WeightCategory
{
    public int WeightCategoryId { get; set; }

    public string Title { get; set; } = null!;

    public decimal MinWeight { get; set; }

    public decimal MaxWeight { get; set; }

    public string? Gender { get; set; }

    public DateTime CreatedOn { get; set; }

    public bool JuniorsOnly { get; set; }

    public string? Slug { get; set; }

    public ICollection<EraWeightCategory> EraWeightCategories { get; } = [];

    public ICollection<Participation> Participations { get; } = [];

    public ICollection<Record> Records { get; } = [];
}