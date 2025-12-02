using KRAFT.Results.Contracts.Users;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class LoginCommandBuilder
{
    private string _username = Constants.TestUser.Username;
    private string _password = Constants.TestUser.Password;

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

    public LoginCommand Build() =>
        new(_username, _password);
}