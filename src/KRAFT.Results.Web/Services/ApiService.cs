namespace KRAFT.Results.Web.Services;

internal sealed class ApiService : IApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public Task<T?> GetAsync<T>(string path) => _httpClient.GetFromJsonAsync<T>(path);
}