using KRAFT.Results.WebApi.Features.AgeCategories;
using KRAFT.Results.WebApi.Features.Athletes;
using KRAFT.Results.WebApi.Features.Attempts;
using KRAFT.Results.WebApi.Features.Meets;
using KRAFT.Results.WebApi.Features.Teams;
using KRAFT.Results.WebApi.Features.WeightCategories;

namespace KRAFT.Results.WebApi.Features.Participations;

internal sealed class Participation
{
    public int ParticipationId { get; set; }

    public int AthleteId { get; set; }

    public int MeetId { get; set; }

    public decimal Weight { get; set; }

    public int WeightCategoryId { get; set; }

    public int? TeamId { get; set; }

    public int AgeCategoryId { get; set; }

    public int Place { get; set; }

    public bool Disqualified { get; set; }

    public decimal Squat { get; set; }

    public decimal Benchpress { get; set; }

    public decimal Deadlift { get; set; }

    public decimal Total { get; set; }

    public decimal Wilks { get; set; }

    public decimal? Ipfpoints { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public string CreatedBy { get; set; } = null!;

    public int LotNo { get; set; }

    public int? TeamPoints { get; set; }

    public AgeCategory AgeCategory { get; set; } = null!;

    public Athlete Athlete { get; set; } = null!;

    public ICollection<Attempt> Attempts { get; } = [];

    public Meet Meet { get; set; } = null!;

    public Team? Team { get; set; }

    public WeightCategory WeightCategory { get; set; } = null!;
}