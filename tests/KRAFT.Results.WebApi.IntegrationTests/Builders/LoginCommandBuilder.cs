namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class LoginCommandBuilder
{
    private string _username = "testuser";
    private string _password = "TestPassword123!";

    public LoginCommandBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public LoginCommandBuilder WithPassword(string password)
    {
        _password = password;
        return this;
    }

    public object Build()
    {
        return new
        {
            Username = _username,
            Password = _password,
        };
    }
}