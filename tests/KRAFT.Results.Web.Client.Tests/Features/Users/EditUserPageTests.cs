using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;

using Bunit;
using Bunit.TestDoubles;

using KRAFT.Results.Contracts.Users;
using KRAFT.Results.Web.Client.Features.Users;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace KRAFT.Results.Web.Client.Tests.Features.Users;

public sealed class EditUserPageTests : IDisposable
{
    private readonly BunitContext _context = new();

    [Fact]
    public void ShowsLoadingSpinner_WhenDataIsBeingFetched()
    {
        // Arrange
        RegisterHttpClient(delay: true);

        // Act
        IRenderedComponent<EditUserPage> cut = _context.Render<EditUserPage>(
            p => p.Add(c => c.UserId, 1));

        // Assert
        cut.Find("[role='status']").ShouldNotBeNull();
        cut.Find(".visually-hidden").TextContent.ShouldBe("Sæki gögn...");
    }

    [Fact]
    public void ShowsNotFoundMessage_WhenUserReturnsNull()
    {
        // Arrange
        RegisterNullUserHttpClient();

        // Act
        IRenderedComponent<EditUserPage> cut = _context.Render<EditUserPage>(
            p => p.Add(c => c.UserId, 99));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find("[role='alert']").TextContent.ShouldContain("fannst ekki");
        });
    }

    [Fact]
    public void ShowsErrorWithRetryButton_WhenHttpRequestFails()
    {
        // Arrange
        RegisterFailingHttpClient();

        // Act
        IRenderedComponent<EditUserPage> cut = _context.Render<EditUserPage>(
            p => p.Add(c => c.UserId, 1));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").ShouldNotBeNull();
            cut.Find(".retry-btn").ShouldNotBeNull();
        });
    }

    [Fact]
    public void ShowsUserForm_WhenLoaded()
    {
        // Arrange
        RegisterHttpClient();

        // Act
        IRenderedComponent<EditUserPage> cut = _context.Render<EditUserPage>(
            p => p.Add(c => c.UserId, 1));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Find("form").ShouldNotBeNull();
        });
    }

    [Fact]
    public void DisablesRoleCheckboxes_WhenEditingSelf()
    {
        // Arrange
        RegisterHttpClient(currentUserId: 1);

        // Act
        IRenderedComponent<EditUserPage> cut = _context.Render<EditUserPage>(
            p => p.Add(c => c.UserId, 1));

        // Assert
        cut.WaitForAssertion(() =>
        {
            IReadOnlyList<AngleSharp.Dom.IElement> roleCheckboxes = cut.FindAll("input[type='checkbox']");
            roleCheckboxes.Count.ShouldBeGreaterThan(0);
            bool allDisabled = roleCheckboxes.All(cb => cb.GetAttribute("disabled") != null);
            allDisabled.ShouldBeTrue();
        });
    }

    [Fact]
    public void EnablesRoleCheckboxes_WhenEditingAnotherUser()
    {
        // Arrange
        RegisterHttpClient(currentUserId: 2);

        // Act
        IRenderedComponent<EditUserPage> cut = _context.Render<EditUserPage>(
            p => p.Add(c => c.UserId, 1));

        // Assert
        cut.WaitForAssertion(() =>
        {
            IReadOnlyList<AngleSharp.Dom.IElement> roleCheckboxes = cut.FindAll("input[type='checkbox']");
            roleCheckboxes.Count.ShouldBeGreaterThan(0);
            bool allEnabled = roleCheckboxes.All(cb => cb.GetAttribute("disabled") is null);
            allEnabled.ShouldBeTrue();
        });
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterHttpClient(bool delay = false, int currentUserId = 99)
    {
        EditUserPageMockHandler handler = new(delay);
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://localhost") };
        _context.Services.AddSingleton(httpClient);
        BunitAuthorizationContext authContext = _context.AddAuthorization();
        authContext.SetAuthorized("test-user");
        authContext.SetClaims(new Claim(ClaimTypes.NameIdentifier, currentUserId.ToString(System.Globalization.CultureInfo.InvariantCulture)));
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2000:Dispose objects before losing scope", Justification = "HttpClient lifetime is managed by the DI container.")]
    private void RegisterNullUserHttpClient()
    {
        NullUserHttpMessageHandler handler = new();
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

    private sealed class EditUserPageMockHandler(bool delay = false) : HttpMessageHandler
    {
        private readonly UserEditDetails _user = new(
            FirstName: "Jon",
            LastName: "Jonsson",
            Email: "jon@example.com",
            Roles: ["Admin"]);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (delay)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(_user),
            };
        }
    }

    private sealed class NullUserHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null", System.Text.Encoding.UTF8, "application/json"),
            });
        }
    }
}