using KRAFT.Results.Core.Participations;
using KRAFT.Results.Core.Records;

namespace KRAFT.Results.Core.AgeCategories;

internal class AgeCategory
{
    public int AgeCategoryId { get; set; }

    public string Title { get; set; } = null!;

    public string? TitleShort { get; set; }

    public DateTime CreatedOn { get; set; }

    public string? Slug { get; set; }

    public virtual ICollection<Participation> Participations { get; set; } = [];

    public virtual ICollection<Record> Records { get; set; } = [];
}