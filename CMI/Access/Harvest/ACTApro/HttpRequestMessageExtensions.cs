using System.Net.Http;

namespace CMI.Access.Harvest.ActaPro;

internal static class HttpRequestMessageExtensions
{
    public static HttpRequestMessage Clone(this HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            Content = request.Content is null ? null : new StreamContent(request.Content.ReadAsStreamAsync().Result)
        };

        foreach (var h in request.Headers)
            clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
        if (request.Content is not null)
            foreach (var h in request.Content.Headers)
                clone.Content!.Headers.TryAddWithoutValidation(h.Key, h.Value);

        return clone;
    }
}