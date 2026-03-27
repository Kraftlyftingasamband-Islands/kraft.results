using KRAFT.Results.Contracts.Meets;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class RecordAttemptCommandBuilder
{
    private decimal _weight = 100.0m;
    private bool _good = true;

    public RecordAttemptCommandBuilder WithWeight(decimal weight)
    {
        _weight = weight;
        return this;
    }

    public RecordAttemptCommandBuilder WithGood(bool good)
    {
        _good = good;
        return this;
    }

    public RecordAttemptCommand Build() => new(_weight, _good);
}