using System.Reflection;

using KRAFT.Results.Web.Client.Features.Users;

using Microsoft.AspNetCore.Authorization;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Users;

public sealed class UserIndexTests
{
    [Fact]
    public void RequiresAdminRole()
    {
        // Arrange
        AuthorizeAttribute? attribute = typeof(UserIndex).GetCustomAttribute<AuthorizeAttribute>();

        // Act

        // Assert
        attribute.ShouldNotBeNull();
        attribute.Roles.ShouldBe("Admin");
    }
}