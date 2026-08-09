using KRAFT.Results.Contracts.Clubs;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class CreateClubCommandBuilder
{
    private string _title = Guid.NewGuid().ToString();
    private string _titleShort = UniqueShortCode.Next();
    private string _titleFull = Guid.NewGuid().ToString();
    private string _countryCode = "ISL";

    public CreateClubCommandBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public CreateClubCommandBuilder WithTitleShort(string titleShort)
    {
        _titleShort = titleShort;
        return this;
    }

    public CreateClubCommandBuilder WithTitleFull(string titleFull)
    {
        _titleFull = titleFull;
        return this;
    }

    public CreateClubCommandBuilder WithCountryCode(string countryCode)
    {
        _countryCode = countryCode;
        return this;
    }

    public CreateClubCommand Build() =>
        new(_title, _titleShort, _titleFull, _countryCode);
}
