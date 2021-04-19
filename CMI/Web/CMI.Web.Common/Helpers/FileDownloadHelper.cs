using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.ServiceModel.Channels;
using System.Text;
using System.Web;

namespace CMI.Web.Common.Helpers
{
    /// <summary>Some common functions used to initiate and process a file download from Public or Management client.</summary>
    public class FileDownloadHelper : IFileDownloadHelper
    {
        private readonly ICmiSettings settings;

        public FileDownloadHelper(ICmiSettings settings)
        {
            this.settings = settings;
        }

        public string CreateDownloadToken()
        {
            const string salt = "Viaduc Download Token Hash";
            var bytes = Encoding.UTF8.GetBytes(salt + Guid.NewGuid().ToString("N") + DateTime.Now.Ticks);
            using (var sha = SHA1.Create())
            {
                return string.Concat(sha.ComputeHash(bytes).Select(b => b.ToString("x2")));
            }
        }

        /// <summary>
        ///     Client ID via API ermitteln
        ///     https://stackoverflow.com/questions/22532806/asp-net-web-api-2-1-get-client-ip-address
        /// </summary>
        public string GetClientIp(HttpRequestMessage request)
        {
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                return ((HttpContextWrapper) request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }

            if (request.Properties.ContainsKey(RemoteEndpointMessageProperty.Name))
            {
                var prop = (RemoteEndpointMessageProperty) request.Properties[RemoteEndpointMessageProperty.Name];
                return prop.Address;
            }

            if (HttpContext.Current != null)
            {
                return HttpContext.Current.Request.UserHostAddress;
            }

            return null;
        }

        public int GetConfigValueTokenValidTime()
        {
            var keyName = "tokenValidTime";
            var configValue = settings[keyName];
            return int.TryParse(configValue, out var result) ? result : 10;
        }
    }
}