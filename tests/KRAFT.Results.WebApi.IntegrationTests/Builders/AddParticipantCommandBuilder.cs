using KRAFT.Results.Contracts.Meets;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class AddParticipantCommandBuilder
{
    private string _athleteSlug = Constants.TestAthleteSlug;
    private decimal _bodyWeight = 80.5m;
    private int? _teamId;

    public AddParticipantCommandBuilder WithAthleteSlug(string slug)
    {
        _athleteSlug = slug;
        return this;
    }

    public AddParticipantCommandBuilder WithBodyWeight(decimal bodyWeight)
    {
        _bodyWeight = bodyWeight;
        return this;
    }

    public AddParticipantCommandBuilder WithTeamId(int? teamId)
    {
        _teamId = teamId;
        return this;
    }

    public AddParticipantCommand Build() =>
        new(_athleteSlug, _bodyWeight, _teamId);
}