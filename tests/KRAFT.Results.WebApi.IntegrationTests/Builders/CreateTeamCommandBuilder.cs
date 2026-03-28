using KRAFT.Results.Contracts.Teams;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class CreateTeamCommandBuilder
{
    private static int _counter;

    private string _title = Guid.NewGuid().ToString();
    private string _titleShort = GenerateUniqueTitleShort();
    private string _titleFull = Guid.NewGuid().ToString();
    private int _countryId = 1;

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

    public CreateTeamCommandBuilder WithCountryId(int countryId)
    {
        _countryId = countryId;
        return this;
    }

    public CreateTeamCommand Build() =>
        new(_title, _titleShort, _titleFull, _countryId);

    private static string GenerateUniqueTitleShort()
    {
        int n = Interlocked.Increment(ref _counter);
        char c0 = (char)('a' + (n % 26));
        char c1 = (char)('a' + ((n / 26) % 26));
        char c2 = (char)('a' + ((n / 676) % 26));
        return new string([c0, c1, c2]);
    }
}