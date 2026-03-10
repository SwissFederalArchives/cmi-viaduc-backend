using System;
using System.Text.Json.Serialization;

namespace CMI.Access.Harvest.ActaPro;

public class ActaProToken
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    internal void CalculateExpiresDate()
    {
        // Subtract 5 seconds to account for network latency
        ExpiresAtUtc = DateTime.UtcNow.AddSeconds(ExpiresIn - 5);
    }
}