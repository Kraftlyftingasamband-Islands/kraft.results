namespace KRAFT.Results.Web.Services;

internal interface IApiService
{
    Task<T?> GetAsync<T>(string path);
}