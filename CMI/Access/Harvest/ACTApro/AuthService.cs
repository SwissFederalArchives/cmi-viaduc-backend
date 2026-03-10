using Serilog;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CMI.Access.Harvest.Properties;

namespace CMI.Access.Harvest.ActaPro;

// Helper: clone request content/headers for retry

public class AuthService : IAuthService
{
    private ActaProToken token;
    private readonly HttpClient actaProTokenHttpClient = new();
    
    public async Task<ActaProToken> GetNewTokenAsync(CancellationToken ct)
    {
        Log.Information("Getting initial access token for ActaPro");

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Settings.Default.ActaProUser}:{Settings.Default.ActaProPassword}"));
        actaProTokenHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var uri = new Uri(Settings.Default.ActaProTokenEndpoint);

        var formaData = new MultipartFormDataContent();
        formaData.Add(new StringContent(Settings.Default.ActaProTokenUser), "username");
        formaData.Add(new StringContent(Settings.Default.ActaProTokenPassword), "password");
        formaData.Add(new StringContent(Settings.Default.ActaProGrantType), "grant_type");

        var response = await actaProTokenHttpClient.PostAsync(uri, formaData, ct);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();

            token = JsonSerializer.Deserialize<ActaProToken>(result);
            token?.CalculateExpiresDate();

            Log.Information("Successfully retrieved the access token for ActaPro");

            return token;
        }

        throw new HttpRequestException($"Failed to get token from ActaPro. Status code: {response.StatusCode}");
    }

    public async Task<ActaProToken> RefreshAsync(string refreshToken, CancellationToken ct)
    {

        Log.Information("Refreshing access token for ActaPro");

        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{Settings.Default.ActaProUser}:{Settings.Default.ActaProPassword}"));
        actaProTokenHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var uri = new Uri(Settings.Default.ActaProTokenEndpoint);

        var formaData = new MultipartFormDataContent();
        formaData.Add(new StringContent("refresh_token"), "grant_type");
        formaData.Add(new StringContent(refreshToken), "refresh_token");

        var response = await actaProTokenHttpClient.PostAsync(uri, formaData, ct);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync();

            token = JsonSerializer.Deserialize<ActaProToken>(result);
            token?.CalculateExpiresDate();

            Log.Information("Successfully refreshed the access token for ActaPro");
            return token;
        }

        throw new HttpRequestException($"Failed to refresh token from ActaPro. Status code: {response.StatusCode}");
    }
}