using System;
using System.Threading;
using System.Threading.Tasks;

namespace CMI.Access.Harvest.ActaPro;

public sealed class TokenProvider
{
    private readonly IAuthService auth;
    private readonly TimeSpan skew = TimeSpan.FromMinutes(5);               // clock skew / early refresh
    private readonly SemaphoreSlim gate = new(1, 1);
    private volatile ActaProToken currentToken;                             // last good tokens

    public TokenProvider(IAuthService auth)
    {
        this.auth = auth;
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken ct)
    {
        var tok = currentToken;
        if (tok is not null && !IsExpiring(tok))
            return tok.AccessToken;

        await gate.WaitAsync(ct);
        try
        {
            // Double-check after acquiring the lock.
            tok = currentToken;
            if (tok is null)
            {
                currentToken = await auth.GetNewTokenAsync(ct);
            }
            else if (IsExpiring(tok))
            {
                try
                {
                    currentToken = await auth.RefreshAsync(tok.RefreshToken, ct);
                }
                catch
                {
                    // Fallback: refresh token might be revoked/rotated -> do a full new token
                    currentToken = await auth.GetNewTokenAsync(ct);
                }
            }
            return currentToken!.AccessToken;
        }
        finally
        {
            gate.Release();
        }
    }

    private bool IsExpiring(ActaProToken t)
    {
        return DateTimeOffset.UtcNow >= (t.ExpiresAtUtc - skew);
    }

    public async Task ForceRefreshAsync(CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            var tok = currentToken;
            currentToken = tok is null
                ? await auth.GetNewTokenAsync(ct)
                : await auth.RefreshAsync(tok.RefreshToken, ct);
        }
        finally
        {
            gate.Release();
        }
    }
}