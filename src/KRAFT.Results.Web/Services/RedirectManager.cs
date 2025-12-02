using Microsoft.AspNetCore.Components;

namespace KRAFT.Results.Web.Services;

internal sealed class RedirectManager(NavigationManager navigationManager)
{
    public void RedirectTo(string? uri)
    {
        uri ??= string.Empty;

        if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
        {
            uri = navigationManager.ToBaseRelativePath(uri);
        }

        navigationManager.NavigateTo(uri);
    }
}