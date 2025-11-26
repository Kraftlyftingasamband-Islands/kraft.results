namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class CreateAthleteCommandBuilder
{
    private string _firstName = Guid.NewGuid().ToString();
    private string _lastName = Guid.NewGuid().ToString();
    private string _gender = "m";
    private DateOnly _dateOfBirth = DateOnly.FromDateTime(DateTime.Today.AddYears(-14));
    private int _countryId = 1;
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

    public CreateAthleteCommandBuilder WithCountryId(int countryId)
    {
        _countryId = countryId;
        return this;
    }

    public CreateAthleteCommandBuilder WithTeamId(int? teamId)
    {
        _teamId = teamId;
        return this;
    }

    public object Build()
    {
        return new
        {
            FirstName = _firstName,
            LastName = _lastName,
            Gender = _gender,
            CountryId = _countryId,
            TeamId = _teamId,
            DateOfBirth = _dateOfBirth,
        };
    }
}