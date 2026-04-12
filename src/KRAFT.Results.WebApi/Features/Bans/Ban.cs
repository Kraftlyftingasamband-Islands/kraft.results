using KRAFT.Results.WebApi.Features.Athletes;

namespace KRAFT.Results.WebApi.Features.Bans;

internal sealed class Ban
{
    public int BanId { get; private set; }

    public int AthleteId { get; private set; }

    public Athlete Athlete { get; private set; } = default!;

    public DateTime FromDate { get; private set; }

    public DateTime ToDate { get; private set; }

    public DateTime CreatedOn { get; private set; }
}