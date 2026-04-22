using KRAFT.Results.WebApi.Features.Bans;

namespace KRAFT.Results.WebApi.UnitTests.Builders;

internal sealed class BanBuilder
{
    private int _athleteId = 1;
    private DateTime _fromDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private DateTime _toDate = new(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc);

    public BanBuilder WithAthleteId(int athleteId)
    {
        _athleteId = athleteId;
        return this;
    }

    public BanBuilder WithFromDate(DateTime fromDate)
    {
        _fromDate = fromDate;
        return this;
    }

    public BanBuilder WithToDate(DateTime toDate)
    {
        _toDate = toDate;
        return this;
    }

    public Ban Build() => Ban.Create(_athleteId, _fromDate, _toDate).FromResult();
}