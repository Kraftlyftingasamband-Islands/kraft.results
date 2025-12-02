using KRAFT.Results.Contracts.Users;

using Microsoft.Extensions.Caching.Memory;

namespace KRAFT.Results.Web.Services;

internal sealed class ApiService : IApiService
{
    private const string AccessTokenCacheKey = "AccessToken";

    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _memoryCache;

    public ApiService(HttpClient httpClient, IMemoryCache memoryCache)
    {
        _httpClient = httpClient;
        _memoryCache = memoryCache;
        if (memoryCache.TryGetValue(AccessTokenCacheKey, out string? accessToken))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        }
    }

    public Task<T?> GetAsync<T>(string path) => _httpClient.GetFromJsonAsync<T>(path);

    public Task<HttpResponseMessage> PostAsync<T>(string path, T body) => _httpClient.PostAsJsonAsync<T>(path, body);

    public async Task<bool> LoginAsync(string username, string password)
    {
        LoginCommand command = new(username, password);
        HttpResponseMessage message = await _httpClient.PostAsJsonAsync("/users/login", command);

        if (!message.IsSuccessStatusCode)
        {
            return false;
        }

        AuthenticatedResponse? response = await message.Content.ReadFromJsonAsync<AuthenticatedResponse>();

        if (response is null || string.IsNullOrWhiteSpace(response.AccessToken))
        {
            return false;
        }

        _memoryCache.Set(AccessTokenCacheKey, response.AccessToken);

        return true;
    }

    public void Logout() => _memoryCache.Remove(AccessTokenCacheKey);
}