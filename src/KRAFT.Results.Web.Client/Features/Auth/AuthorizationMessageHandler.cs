using System.Net.Http.Headers;

namespace KRAFT.Results.Web.Client.Features.Auth;

public sealed class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly TokenStorageService _tokenStorage;
    private readonly Uri _allowedBaseUri;

    public AuthorizationMessageHandler(TokenStorageService tokenStorage, Uri allowedBaseUri)
    {
        _tokenStorage = tokenStorage;
        _allowedBaseUri = allowedBaseUri;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.RequestUri is not null &&
            request.RequestUri.AbsoluteUri.StartsWith(_allowedBaseUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase))
        {
            string? token = await _tokenStorage.GetTokenAsync();

            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}