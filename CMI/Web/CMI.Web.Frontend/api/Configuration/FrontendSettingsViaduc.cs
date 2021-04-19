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
    }
}