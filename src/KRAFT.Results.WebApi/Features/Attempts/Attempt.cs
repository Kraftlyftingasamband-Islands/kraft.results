using KRAFT.Results.Core.Features.Participations;
using KRAFT.Results.Core.Features.Records;

namespace KRAFT.Results.Core.Features.Attempts;

internal sealed class Attempt
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

    public Participation Participation { get; set; } = null!;

    public ICollection<Record> Records { get; } = [];
}