using KRAFT.Results.WebApi.Features.EraWeightCategories;
using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records;
using KRAFT.Results.WebApi.ValueObjects;

namespace KRAFT.Results.WebApi.Features.WeightCategories;

internal sealed class WeightCategory
{
    public int WeightCategoryId { get; set; }

    public required string Title { get; set; }

    public required decimal MinWeight { get; set; }

    public required decimal MaxWeight { get; set; }

    public required Gender Gender { get; set; }

    public required DateTime CreatedOn { get; set; }

    public bool JuniorsOnly { get; set; }

    public required string Slug { get; set; }

    public ICollection<EraWeightCategory> EraWeightCategories { get; } = [];

    public ICollection<Participation> Participations { get; } = [];

    public ICollection<Record> Records { get; } = [];
}