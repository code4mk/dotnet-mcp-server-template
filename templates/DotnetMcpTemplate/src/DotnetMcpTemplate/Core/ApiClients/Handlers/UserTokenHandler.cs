using System.Net.Http.Headers;
using DotnetMcpTemplate.Core.Auth;

namespace DotnetMcpTemplate.Core.ApiClients.Handlers;

/// <summary>{PREFIX}_AUTH=user: calls the API as the signed-in user with their IdP access token.</summary>
public sealed class UserTokenHandler(IUpstreamTokenAccessor tokens, string prefix) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokens.GetAccessTokenAsync(cancellationToken)
            ?? throw new ApiClientException(prefix,
                "This data source needs your sign-in, but no identity provider token is available. Sign in again.");

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
