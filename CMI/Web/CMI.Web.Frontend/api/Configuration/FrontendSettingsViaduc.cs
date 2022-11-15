using System.Reflection;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;

namespace CMI.Web.Frontend.api.Configuration
{
    public class FrontendSettingsViaduc : AppSettings
    {
        static FrontendSettingsViaduc()
        {
            Instance = new FrontendSettingsViaduc();
        }

        private FrontendSettingsViaduc()
            : base(Assembly.GetExecutingAssembly())
        {
        }

        public static FrontendSettingsViaduc Instance { get; }
        public string SqlConnectionString { get; } = WebHelper.Settings["sqlConnectionString"];
        public string SqlConnectionStringEF { get; } = WebHelper.Settings["sqlConnectionStringEF"];
        public int CookieExpireTimeInMinutes { get; } = WebHelper.GetIntSetting("cookieExpireTimeInMinutes", 60);
    }
}
