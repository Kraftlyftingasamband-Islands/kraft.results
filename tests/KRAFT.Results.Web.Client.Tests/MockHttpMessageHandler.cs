using System.Net;
using System.Net.Http.Json;

using KRAFT.Results.Contracts.Athletes;

namespace KRAFT.Results.Web.Client.Tests;

internal sealed class MockHttpMessageHandler(List<AthleteSummary> athletes, bool delay = false) : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (delay)
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        HttpResponseMessage response = new(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(athletes),
        };

        return response;
    }
}