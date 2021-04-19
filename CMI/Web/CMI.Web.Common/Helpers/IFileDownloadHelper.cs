using System.Net.Http;

namespace CMI.Web.Common.Helpers
{
    public interface IFileDownloadHelper
    {
        string CreateDownloadToken();
        string GetClientIp(HttpRequestMessage request);

        int GetConfigValueTokenValidTime();
    }
}