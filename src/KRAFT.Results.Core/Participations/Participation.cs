using KRAFT.Results.Core.AgeCategories;
using KRAFT.Results.Core.Athletes;
using KRAFT.Results.Core.Attempts;
using KRAFT.Results.Core.Meets;
using KRAFT.Results.Core.Teams;
using KRAFT.Results.Core.WeightCategories;

namespace KRAFT.Results.Core.Participations;

internal class Participation
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

    public virtual AgeCategory AgeCategory { get; set; } = null!;

    public virtual Athlete Athlete { get; set; } = null!;

    public virtual ICollection<Attempt> Attempts { get; } = [];

    public virtual Meet Meet { get; set; } = null!;

    public virtual Team? Team { get; set; }

    public virtual WeightCategory WeightCategory { get; set; } = null!;
}