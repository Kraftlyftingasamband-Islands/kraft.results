namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class CreateUserCommandBuilder
{
    private string _username = Guid.NewGuid().ToString();
    private string _firstName = Guid.NewGuid().ToString();
    private string _lastName = Guid.NewGuid().ToString();
    private string _email = $"{Guid.NewGuid()}@{Guid.NewGuid()}";
    private string _password = Guid.NewGuid().ToString();

    public CreateUserCommandBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public CreateUserCommandBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public CreateUserCommandBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public CreateUserCommandBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public CreateUserCommandBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public object Build()
    {
        return new
        {
            Username = _username,
            FirstName = _firstName,
            LastName = _lastName,
            Email = _email,
            Password = _password,
        };
    }
}