using Microsoft.JSInterop;

namespace KRAFT.Results.Web.Client.Features.Auth;

public sealed class TokenStorageService
{
    private const string TokenKey = "auth_token";

    private readonly IJSRuntime _jsRuntime;
    private string? _cachedToken;

    public TokenStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public async Task<string?> GetTokenAsync()
    {
        if (_cachedToken is not null)
        {
            return _cachedToken;
        }

        try
        {
            _cachedToken = await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", TokenKey);
            return _cachedToken;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    public async Task SetTokenAsync(string token)
    {
        _cachedToken = token;
        await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", TokenKey, token);
    }

    public async Task RemoveTokenAsync()
    {
        _cachedToken = null;

        try
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", TokenKey);
        }
        catch (InvalidOperationException)
        {
            // JS interop is unavailable during SSR pre-rendering
        }
    }
}