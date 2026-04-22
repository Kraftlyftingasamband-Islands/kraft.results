using System.Reflection;

using KRAFT.Results.WebApi.Features.Users;

namespace KRAFT.Results.WebApi.UnitTests.Builders;

// User.Create() requires a User creator parameter, creating a chicken-and-egg problem.
// Reflection is encapsulated here so that test files can create User instances without
// duplicating reflection code.
internal sealed class UserBuilder
{
    private string _username = "testuser";

    public UserBuilder WithUsername(string username)
    {
        _username = username;
        return this;
    }

    public User Build()
    {
        User user = (User)Activator.CreateInstance(typeof(User), nonPublic: true)!;
        PropertyInfo usernameProperty = typeof(User).GetProperty(nameof(User.Username))!;
        usernameProperty.SetValue(user, _username);
        return user;
    }
}