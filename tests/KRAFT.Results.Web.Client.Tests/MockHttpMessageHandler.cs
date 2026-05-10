using System.Net;
using System.Net.Http.Json;

namespace KRAFT.Results.Web.Client.Tests;

internal sealed class MockHttpMessageHandler<T>(List<T> items, bool delay = false) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (delay)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(items),
        };

        return response;
    }
}