using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CMI.Access.Harvest.ActaPro;

public sealed class AuthHandler : DelegatingHandler
{
    private readonly TokenProvider tokens;

    public AuthHandler(TokenProvider tokens)
    {
        this.tokens = tokens;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        await AttachBearerAsync(request, ct);
        var response = await base.SendAsync(request, ct);

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            response.Dispose();

            // Force a refresh (handles rotated/expired/invalid tokens), then retry once.
            await tokens.ForceRefreshAsync(ct);

            var retry = request.Clone(); // see helper below
            await AttachBearerAsync(retry, ct);
            return await base.SendAsync(retry, ct);
        }

        return response;
    }

    private async Task AttachBearerAsync(HttpRequestMessage req, CancellationToken ct)
    {
        var token = await tokens.GetAccessTokenAsync(ct);
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }
}