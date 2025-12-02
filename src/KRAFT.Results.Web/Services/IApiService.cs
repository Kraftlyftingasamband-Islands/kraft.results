namespace KRAFT.Results.Web.Services;

internal interface IApiService
{
    Task<T?> GetAsync<T>(string path);

    Task<HttpResponseMessage> PostAsync<T>(string path, T body);

    Task<bool> LoginAsync(string username, string password);

    void Logout();
}