using System.Reflection;

using Bunit;

using KRAFT.Results.Contracts.Users;
using KRAFT.Results.Web.Client.Features.Users;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Users;

public sealed class UserIndexTests : IDisposable
{
    private readonly BunitContext _context = new();

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

    [Fact]
    public void ShowsErrorWithRetryButton_WhenHttpRequestFails()
    {
        // Arrange
        RegisterFailingHttpClient();

        // Act
        IRenderedComponent<UserIndex> cut = _context.Render<UserIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find(".retry-btn").ShouldNotBeNull();
        });
    }

    [Fact]
    public void AlwaysShowsCreateUserButtonInHeader()
    {
        // Arrange
        RegisterHttpClient([]);

        // Act
        IRenderedComponent<UserIndex> cut = _context.Render<UserIndex>();

        // Assert
        cut.Find("button.btn-action").ShouldNotBeNull();
        cut.Find("button.btn-action").TextContent.ShouldContain("Stofna notanda");
    }

    [Fact]
    public void ShowsUserTable_WhenUsersAreLoaded()
    {
        // Arrange
        List<UserSummary> users =
        [
            new(1, "Jon Jonsson", "jon@example.com", DateTime.Today, ["Admin"]),
            new(2, "Anna Karlsdottir", "anna@example.com", DateTime.Today, ["User"]),
        ];
        RegisterHttpClient(users);

        // Act
        IRenderedComponent<UserIndex> cut = _context.Render<UserIndex>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("table tbody tr").Count.ShouldBe(2);
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterHttpClient(List<UserSummary> users)
    {
        MockHttpMessageHandler<UserSummary> handler = new(users);
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterFailingHttpClient()
    {
        FailingHttpMessageHandler handler = new();
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        _context.AddAuthorization();
    }
}