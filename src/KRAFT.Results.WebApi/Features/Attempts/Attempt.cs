using KRAFT.Results.Contracts;
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

    public Discipline Discipline { get; private set; }

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
        Discipline discipline,
        short round,
        decimal weight,
        bool good,
        string createdBy)
    {
        return new Attempt
        {
            ParticipationId = participationId,
            Discipline = discipline,
            Round = round,
            Weight = weight,
            Good = good,
            CreatedBy = createdBy,
            CreatedOn = DateTime.UtcNow,
            ModifiedBy = createdBy,
            ModifiedOn = DateTime.UtcNow,
        };
    }

    internal void Update(decimal weight, bool good, string modifiedBy)
    {
        Weight = weight;
        Good = good;
        ModifiedBy = modifiedBy;
        ModifiedOn = DateTime.UtcNow;
    }
}