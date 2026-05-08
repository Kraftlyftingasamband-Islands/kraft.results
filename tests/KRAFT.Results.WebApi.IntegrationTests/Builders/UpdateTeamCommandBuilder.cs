using KRAFT.Results.Contracts.Teams;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class UpdateTeamCommandBuilder
{
    private string _title = Guid.NewGuid().ToString();
    private string _titleShort = UniqueShortCode.Next();
    private string _titleFull = Guid.NewGuid().ToString();
    private string _countryCode = "ISL";

    public UpdateTeamCommandBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public UpdateTeamCommandBuilder WithTitleShort(string titleShort)
    {
        _titleShort = titleShort;
        return this;
    }

    public UpdateTeamCommandBuilder WithTitleFull(string titleFull)
    {
        _titleFull = titleFull;
        return this;
    }

    public UpdateTeamCommandBuilder WithCountryCode(string countryCode)
    {
        _countryCode = countryCode;
        return this;
    }

    public UpdateTeamCommand Build() =>
        new(_title, _titleShort, _titleFull, _countryCode);
}