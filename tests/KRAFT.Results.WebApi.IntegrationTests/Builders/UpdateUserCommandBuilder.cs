using KRAFT.Results.Contracts.Users;

namespace KRAFT.Results.WebApi.IntegrationTests.Builders;

internal sealed class UpdateUserCommandBuilder
{
    private string _firstName = Guid.NewGuid().ToString()[..20];
    private string _lastName = Guid.NewGuid().ToString()[..20];
    private string _email = $"{Guid.NewGuid()}@{Guid.NewGuid()}";

    public UpdateUserCommandBuilder WithFirstName(string firstName)
    {
        _firstName = firstName;
        return this;
    }

    public UpdateUserCommandBuilder WithLastName(string lastName)
    {
        _lastName = lastName;
        return this;
    }

    public UpdateUserCommandBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public UpdateUserCommand Build() =>
        new(_firstName, _lastName, _email);
}