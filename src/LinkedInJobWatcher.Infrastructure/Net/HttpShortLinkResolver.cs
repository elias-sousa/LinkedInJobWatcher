using LinkedInJobWatcher.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace LinkedInJobWatcher.Infrastructure.Net;

public sealed class HttpShortLinkResolver(IHttpClientFactory httpClientFactory, ILogger<HttpShortLinkResolver> logger) : IShortLinkResolver
{
    public async Task<string> ResolveAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var client = httpClientFactory.CreateClient(nameof(HttpShortLinkResolver));
            client.Timeout = TimeSpan.FromSeconds(8);

            using var request = new HttpRequestMessage(HttpMethod.Head, url);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            return response.RequestMessage?.RequestUri?.ToString() ?? url;
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Não foi possível resolver o link encurtado {Url}; mantendo o original.", url);
            return url;
        }
    }
}
