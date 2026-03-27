using KRAFT.Results.Contracts.Meets;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class RecordAttemptCommandBuilder
{
    private decimal _weight = 100.0m;

    public RecordAttemptCommandBuilder WithWeight(decimal weight)
    {
        _weight = weight;
        return this;
    }

    public RecordAttemptCommand Build() => new(_weight);
}