namespace KRAFT.Results.WebApi.Services;

internal interface IHttpContextService
{
    string GetUserName();

    bool IsInRole(string role);
}