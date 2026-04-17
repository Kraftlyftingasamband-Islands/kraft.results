using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Users;
using KRAFT.Results.WebApi.IntegrationTests.Builders;
using KRAFT.Results.WebApi.IntegrationTests.Collections;

using Shouldly;

namespace KRAFT.Results.WebApi.IntegrationTests.Features.Users;

[Collection(nameof(UsersCollection))]
public sealed class GetUsersTest(CollectionFixture fixture) : IAsyncLifetime
{
    private const string Path = "/users";

    private readonly HttpClient _authorizedHttpClient = fixture.CreateAuthorizedHttpClient();
    private readonly HttpClient _nonAdminHttpClient = fixture.CreateNonAdminAuthorizedHttpClient();
    private readonly HttpClient _unauthorizedHttpClient = fixture.Factory!.CreateClient();

    private int _userId;
    private string _email = string.Empty;

    public async ValueTask InitializeAsync()
    {
        CreateUserCommand createCommand = new CreateUserCommandBuilder().Build();
        HttpResponseMessage createResponse = await _authorizedHttpClient.PostAsJsonAsync(Path, createCommand, CancellationToken.None);
        createResponse.EnsureSuccessStatusCode();
        _email = createCommand.Email;
        List<UserSummary>? users = await _authorizedHttpClient.GetFromJsonAsync<List<UserSummary>>(Path, CancellationToken.None);
        _userId = users!.First(u => u.Email == _email).UserId;
    }

    public async ValueTask DisposeAsync()
    {
        if (_userId != 0)
        {
            try
            {
                await _authorizedHttpClient.DeleteAsync($"{Path}/{_userId}", CancellationToken.None);
            }
            catch (HttpRequestException)
            {
                // Best-effort cleanup; do not mask test failures.
            }
        }

        _authorizedHttpClient.Dispose();
        _nonAdminHttpClient.Dispose();
        _unauthorizedHttpClient.Dispose();
    }

    [Fact]
    public async Task ReturnsOk()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _authorizedHttpClient.GetAsync(Path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Deserializes()
    {
        // Arrange

        // Act
        IReadOnlyList<UserSummary>? response = await _authorizedHttpClient.GetFromJsonAsync<IReadOnlyList<UserSummary>>(Path, CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task ReturnsTestUser()
    {
        // Arrange

        // Act
        IReadOnlyList<UserSummary>? response = await _authorizedHttpClient.GetFromJsonAsync<IReadOnlyList<UserSummary>>(Path, CancellationToken.None);

        // Assert
        response!.ShouldContain(x => x.Email == _email);
    }

    [Fact]
    public async Task ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _nonAdminHttpClient.GetAsync(Path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ReturnsUnauthorized_WhenHttpClientIsUnauthorized()
    {
        // Arrange

        // Act
        HttpResponseMessage response = await _unauthorizedHttpClient.GetAsync(Path, CancellationToken.None);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}