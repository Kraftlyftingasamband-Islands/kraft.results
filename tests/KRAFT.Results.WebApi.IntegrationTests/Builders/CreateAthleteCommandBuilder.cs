using KRAFT.Results.Contracts.Athletes;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class CreateAthleteCommandBuilder
{
    private string _firstName = Guid.NewGuid().ToString()[..20];
    private string _lastName = Guid.NewGuid().ToString()[..20];
    private string _gender = "m";
    private DateOnly _dateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-14));
    private string _countryCode = "ISL";
    private int? _teamId;

    public CreateAthleteCommandBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public CreateAthleteCommandBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public CreateAthleteCommandBuilder WithGender(string gender)
    {
        _gender = gender;
        return this;
    }

    public CreateAthleteCommandBuilder WithDateOfBirth(DateOnly dateOfBirth)
    {
        _dateOfBirth = dateOfBirth;
        return this;
    }

    public CreateAthleteCommandBuilder WithCountryCode(string countryCode)
    {
        _countryCode = countryCode;
        return this;
    }

    public CreateAthleteCommandBuilder WithTeamId(int? teamId)
    {
        _teamId = teamId;
        return this;
    }

    public CreateAthleteCommand Build() =>
        new(_firstName, _lastName, _countryCode, _teamId, _dateOfBirth, _gender);
}