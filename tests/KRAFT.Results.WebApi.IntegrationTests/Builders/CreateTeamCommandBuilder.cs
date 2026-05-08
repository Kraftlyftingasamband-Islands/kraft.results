using KRAFT.Results.Contracts.Teams;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class CreateTeamCommandBuilder
{
    private string _title = Guid.NewGuid().ToString();
    private string _titleShort = UniqueShortCode.Next();
    private string _titleFull = Guid.NewGuid().ToString();
    private string _countryCode = "ISL";

    public CreateTeamCommandBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public CreateTeamCommandBuilder WithTitleShort(string titleShort)
    {
        _titleShort = titleShort;
        return this;
    }

    public CreateTeamCommandBuilder WithTitleFull(string titleFull)
    {
        _titleFull = titleFull;
        return this;
    }

    public CreateTeamCommandBuilder WithCountryCode(string countryCode)
    {
        _countryCode = countryCode;
        return this;
    }

    public CreateTeamCommand Build() =>
        new(_title, _titleShort, _titleFull, _countryCode);
}