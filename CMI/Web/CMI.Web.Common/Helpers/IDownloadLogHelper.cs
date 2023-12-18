using System.Net.Http;

namespace CMI.Web.Common.Helpers
{
    public interface IDownloadLogHelper
    {
        string CreateLogToken();
        string GetClientIp(HttpRequestMessage request);

        int GetConfigValueTokenValidTime();
    }
}