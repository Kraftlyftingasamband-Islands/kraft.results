using KRAFT.Results.Contracts.Athletes;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class UpdateAthleteCommandBuilder
{
    private string _firstName = Guid.NewGuid().ToString()[..20];
    private string _lastName = Guid.NewGuid().ToString()[..20];
    private string _gender = "m";
    private DateOnly _dateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-14));
    private string _countryCode = "ISL";
    private int? _teamId;

    public UpdateAthleteCommandBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public UpdateAthleteCommandBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public UpdateAthleteCommandBuilder WithGender(string gender)
    {
        _gender = gender;
        return this;
    }

    public UpdateAthleteCommandBuilder WithDateOfBirth(DateOnly dateOfBirth)
    {
        _dateOfBirth = dateOfBirth;
        return this;
    }

    public UpdateAthleteCommandBuilder WithCountryCode(string countryCode)
    {
        _countryCode = countryCode;
        return this;
    }

    public UpdateAthleteCommandBuilder WithTeamId(int? teamId)
    {
        _teamId = teamId;
        return this;
    }

    public UpdateAthleteCommand Build() =>
        new(_firstName, _lastName, _countryCode, _teamId, _dateOfBirth, _gender);
}