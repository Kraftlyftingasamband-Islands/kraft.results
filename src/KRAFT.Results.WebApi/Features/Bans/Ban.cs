using KRAFT.Results.WebApi.Abstractions;
using KRAFT.Results.WebApi.Features.Athletes;

namespace KRAFT.Results.WebApi.Features.Bans;

internal sealed class Ban
{
    // For EF Core
    private Ban()
    {
    }

    public int BanId { get; private set; }

    public int AthleteId { get; private set; }

    public Athlete Athlete { get; private set; } = default!;

    public DateTime FromDate { get; private set; }

    public DateTime ToDate { get; private set; }

    public DateTime CreatedOn { get; private set; }

    internal static Result<Ban> Create(int athleteId, DateTime fromDate, DateTime toDate)
    {
        if (fromDate > toDate)
        {
            return BanErrors.FromDateAfterToDate;
        }

        Ban ban = new()
        {
            AthleteId = athleteId,
            FromDate = fromDate,
            ToDate = toDate,
            CreatedOn = DateTime.UtcNow,
        };

        return ban;
    }
}