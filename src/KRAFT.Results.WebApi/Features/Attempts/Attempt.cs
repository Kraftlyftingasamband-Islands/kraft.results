using KRAFT.Results.WebApi.Features.Participations;
using KRAFT.Results.WebApi.Features.Records;

namespace KRAFT.Results.WebApi.Features.Attempts;

internal sealed class Attempt
{
    // For EF Core
    private Attempt()
    {
    }

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

    internal static Attempt Create(
        int participationId,
        byte disciplineId,
        short round,
        decimal weight,
        string createdBy)
    {
        return new Attempt
        {
            ParticipationId = participationId,
            DisciplineId = disciplineId,
            Round = round,
            Weight = Math.Abs(weight),
            Good = weight > 0,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = createdBy,
            ModifiedOn = DateTime.UtcNow,
        };
    }

    internal void Update(decimal weight, string modifiedBy)
    {
        Weight = Math.Abs(weight);
        Good = weight > 0;
        ModifiedBy = modifiedBy;
        ModifiedOn = DateTime.UtcNow;
    }
}