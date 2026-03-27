using KRAFT.Results.Contracts.Meets;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class AddParticipantCommandBuilder
{
    private int _athleteId = 1;
    private int _weightCategoryId = 1;
    private decimal? _bodyWeight = 80.5m;

    public AddParticipantCommandBuilder WithAthleteId(int athleteId)
    {
        _athleteId = athleteId;
        return this;
    }

    public AddParticipantCommandBuilder WithWeightCategoryId(int weightCategoryId)
    {
        _weightCategoryId = weightCategoryId;
        return this;
    }

    public AddParticipantCommandBuilder WithBodyWeight(decimal? bodyWeight)
    {
        _bodyWeight = bodyWeight;
        return this;
    }

    public AddParticipantCommand Build() =>
        new(_athleteId, _weightCategoryId, _bodyWeight);
}