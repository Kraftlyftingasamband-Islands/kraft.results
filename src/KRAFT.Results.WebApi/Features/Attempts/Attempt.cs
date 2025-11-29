using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records;

namespace KRAFT.Results.WebApi.Features.Attempts;

internal sealed class Attempt
{
    public int AttemptId { get; private set; }

    public int ParticipationId { get; private set; }

    public byte DisciplineId { get; private set; }

    public short Round { get; private set; }

    public decimal Weight { get; private set; }

    public bool Good { get; private set; }

    public DateTime CreatedOn { get; private set; }

    public DateTime ModifiedOn { get; private set; }

    public string ModifiedBy { get; private set; } = null!;

    public string CreatedBy { get; private set; } = null!;

    public Participation Participation { get; private set; } = null!;

    public ICollection<Record> Records { get; } = [];
}