using System.Reflection;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;

namespace CMI.Web.Management.api.Configuration
{
    public class ManagementSettingsViaduc : AppSettings
    {
        static ManagementSettingsViaduc()
        {
            Instance = new ManagementSettingsViaduc();
        }

        private ManagementSettingsViaduc()
            : base(Assembly.GetExecutingAssembly())
        {
        }

        public static ManagementSettingsViaduc Instance { get; }

        public string SqlConnectionString { get; } = WebHelper.Settings["sqlConnectionString"];
    }
}