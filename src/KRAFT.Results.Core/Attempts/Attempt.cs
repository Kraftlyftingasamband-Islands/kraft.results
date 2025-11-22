using KRAFT.Results.Core.Participations;
using KRAFT.Results.Core.Records;

namespace KRAFT.Results.Core.Attempts;

internal class Attempt
{
    public int AttemptId { get; set; }

    public int ParticipationId { get; set; }

    public byte DisciplineId { get; set; }

    public short Round { get; set; }

    public decimal Weight { get; set; }

    public bool Good { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime ModifiedOn { get; set; }

    public string ModifiedBy { get; set; } = null!;

    public string CreatedBy { get; set; } = null!;

    public virtual Participation Participation { get; set; } = null!;

    public virtual ICollection<Record> Records { get; } = [];
}