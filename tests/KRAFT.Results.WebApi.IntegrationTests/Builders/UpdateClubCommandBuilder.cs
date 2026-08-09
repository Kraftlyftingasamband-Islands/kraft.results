using KRAFT.Results.Contracts.Clubs;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class UpdateClubCommandBuilder
{
    private string _title = Guid.NewGuid().ToString();
    private string _titleShort = UniqueShortCode.Next();
    private string _titleFull = Guid.NewGuid().ToString();
    private string _countryCode = "ISL";

    public UpdateClubCommandBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public UpdateClubCommandBuilder WithTitleShort(string titleShort)
    {
        _titleShort = titleShort;
        return this;
    }

    public UpdateClubCommandBuilder WithTitleFull(string titleFull)
    {
        _titleFull = titleFull;
        return this;
    }

    public UpdateClubCommandBuilder WithCountryCode(string countryCode)
    {
        _countryCode = countryCode;
        return this;
    }

    public UpdateClubCommand Build() =>
        new(_title, _titleShort, _titleFull, _countryCode);
}
