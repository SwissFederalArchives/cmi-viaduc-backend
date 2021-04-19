using System;
using System.IO;
using System.Linq;
using CMI.Utilities.Common.Helpers;
using Serilog;

namespace CMI.Web.Common.Helpers
{
    public class DirectoryHelper : IDirectoryHelper
    {
        private string clientConfigDirectory;

        private string clientDefaultPath;

        private string configDirectory;

        private string indexPagePath;

        private string staticDefaultPath;

        private string staticPagePath;
        public static IDirectoryHelper Instance { get; } = new DirectoryHelper();

        public string ClientDefaultPath
        {
            get
            {
                if (string.IsNullOrEmpty(clientDefaultPath))
                {
                    clientDefaultPath = GetPathToLatestVersion(ServiceHelper.Settings?["clientDefaultPath"] ?? "/client/");
                    Log.Information("Setting ClientDefaultPath to {clientDefaultPath}", clientDefaultPath);
                }

                return clientDefaultPath;
            }
        }

        public string StaticPagePath
        {
            get
            {
                if (string.IsNullOrEmpty(staticPagePath))
                {
                    staticPagePath = GetPathToLatestVersion(ServiceHelper.Settings?["staticPagePath"] ?? "~/content/");
                    Log.Information("Setting StaticPagePath to {staticPagePath}", staticPagePath);
                }

                return staticPagePath;
            }
        }

        public string StaticDefaultPath
        {
            get
            {
                if (string.IsNullOrEmpty(staticDefaultPath))
                {
                    staticDefaultPath = GetPathToLatestVersion(ServiceHelper.Settings?["staticDefaultPath"] ?? "/content/");
                    Log.Information("Setting StaticDefaultPath to {staticDefaultPath}", staticDefaultPath);
                }

                return staticDefaultPath;
            }
        }

        public string IndexPagePath
        {
            get
            {
                if (string.IsNullOrEmpty(indexPagePath))
                {
                    var path = ServiceHelper.Settings != null && !string.IsNullOrWhiteSpace(ServiceHelper.Settings["indexPagePath"])
                        ? ServiceHelper.Settings["indexPagePath"]
                        : "~/client/index.html";
                    indexPagePath = path.Replace("/client/", ClientDefaultPath);
                    Log.Information("Setting IndexPagePath to {indexPagePath}", indexPagePath);
                }

                return indexPagePath;
            }
        }

        public string ConfigDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(configDirectory))
                {
                    var configDir = ServiceHelper.Settings != null && !string.IsNullOrWhiteSpace(ServiceHelper.Settings["configDirectory"])
                        ? ServiceHelper.Settings["configDirectory"]
                        : "~/App_Data";
                    configDirectory = WebHelper.MapPathIfNeeded(configDir).TrimEnd('\\') + @"\";
                    Log.Information("Setting ConfigDirectory to {configDirectory}", configDirectory);
                }

                return configDirectory;
            }
        }

        public string ClientConfigDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(clientConfigDirectory))
                {
                    var configDir = ServiceHelper.Settings != null && !string.IsNullOrWhiteSpace(ServiceHelper.Settings["clientConfigDirectory"])
                        ? ServiceHelper.Settings["clientConfigDirectory"]
                        : "~/client/config";
                    configDir = configDir.Replace("/client/", ClientDefaultPath);
                    clientConfigDirectory = WebHelper.MapPathIfNeeded(configDir).TrimEnd('\\') + @"\";
                    Log.Information("Setting ClientConfigDirectory to {clientConfigDirectory}", clientConfigDirectory);
                }

                return clientConfigDirectory;
            }
        }

        private static string GetPathToLatestVersion(string relativePath)
        {
            var path = relativePath;
            try
            {
                var dirPath = WebHelper.MapPathIfNeeded("~" + relativePath);
                var dirParts = dirPath.Split(new[] {'\\'}, StringSplitOptions.RemoveEmptyEntries);
                var dirParent = string.Join(@"\", dirParts.Take(dirParts.Length - 1));
                if (Directory.Exists(dirParent))
                {
                    var dirPrefix = dirPath.TrimEnd('\\');
                    var directory = Directory.GetDirectories(dirParent).Where(p => p.StartsWith(dirPrefix))
                        .OrderByDescending(p => p, NaturalComparer.Instance).FirstOrDefault();
                    if (directory != null)
                    {
                        path = "/" + Path.GetFileName(directory) + "/";
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get latest path for {relativePath}", relativePath);
            }

            return path;
        }
    }
}