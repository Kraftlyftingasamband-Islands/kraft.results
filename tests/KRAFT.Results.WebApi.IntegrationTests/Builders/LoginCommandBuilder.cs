namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class LoginCommandBuilder
{
    private string _username = Constants.TestUsername;
    private string _password = Constants.TestPassword;

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