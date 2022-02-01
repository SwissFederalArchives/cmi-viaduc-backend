using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using MassTransit;
using Rebex;
using Rebex.IO;
using Rebex.Net;
using Serilog;

namespace CMI.Utilities.Cache.Access
{
    public class CacheHelper : ICacheHelper
    {
        public CacheHelper(string sftpLicenseKey)
        {
            Licensing.Key = sftpLicenseKey;
        }

        public async Task<bool> SaveToCache(IBus bus, CacheRetentionCategory retentionCategory, string file)
        {
            if (!File.Exists(file))
            {
                throw new FileNotFoundException("Unable to upload file to cache because file was not found", file);
            }

            var cacheConnectionInfoRequest = new CacheConnectionInfoRequest();
            var requestClient = CreateRequestClient<CacheConnectionInfoRequest>(bus, BusConstants.CacheConnectionInfoRequestQueue);
            var response = (await requestClient.GetResponse<CacheConnectionInfoResponse>(cacheConnectionInfoRequest)).Message;

            try
            {
                using (var client = new Sftp())
                {
                    client.Connect(response.Machine, (int) response.Port);
                    Log.Information("connected to {host}:{port}", response.Machine, response.Port);

                    client.Login(retentionCategory.ToString(), response.Password);

                    Log.Information("{username} logged in successfully ", retentionCategory);
                    client.Upload(file, "/", TraversalMode.NonRecursive, TransferMethod.Copy, ActionOnExistingFiles.OverwriteAll);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpexted error while uploading to cache");
            }

            return false;
        }

        public async Task<string> GetFtpUrl(IBus bus, CacheRetentionCategory retentionCategory, string id)
        {
            Log.Information("GetDownloadLink started");
            var cacheConnectionInfoRequest = new CacheConnectionInfoRequest();
            var requestClient = CreateRequestClient<CacheConnectionInfoRequest>(bus, BusConstants.CacheConnectionInfoRequestQueue);

            var response = (await requestClient.GetResponse<CacheConnectionInfoResponse>(cacheConnectionInfoRequest)).Message;
            Log.Information("GetDownloadLink finished");
            return $"sftp://{retentionCategory}:{response.Password}@{response.Machine}:{response.Port}/{id}";
        }

        public Stream GetStreamFromCache(string sftpUrl)
        {
            Log.Information("Preparing for Downloading from sftpUrl {sftpUrl}", sftpUrl);

            var regexObj =
                new Regex(@"sftp://(?<username>\w+):(?<password>\w*)@(?<host>\S+):(?<port>\d+)/(?<record>\w+)",
                    RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

            var match = regexObj.Match(sftpUrl);

            if (!match.Success)
            {
                throw new ArgumentException(
                    sftpUrl + " does not adhere to the pattern sftp://user:pw@server:port/file", nameof(sftpUrl));
            }

            try
            {
                var client = new Sftp();

                var username = match.Groups["username"].Value;
                var password = match.Groups["password"].Value;
                var host = match.Groups["host"].Value;
                var port = int.Parse(match.Groups["port"].Value);
                var record = match.Groups["record"].Value;

                Log.Information("sftpUrl is wellformed");

                client.Connect(host, port);
                Log.Information("connected to {host}:{port}", host, port);

                client.Login(username, password);
                Log.Information("{username} logged in successfully ", username);

                var stream = client.GetDownloadStream(record);
                Log.Information("got stream for file '{record}' ", username);
                return stream;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "an error ocuured while downloading");
                throw;
            }
        }

        /// <summary>
        ///     Diese Methode wurde nach den genauen schriftlichen Vorgaben des BARs implementiert.
        ///     Diese Vorgaben sind im Anwendungshandbuch enthalten und finden sich auch unter dem folgenden Link:
        /// </summary>
        public async Task<CacheRetentionCategory> GetRetentionCategory(ElasticArchiveRecord archiveRecord, string rolePublicClient,
            IOrderDataAccess orderDataAccess)
        {
            var downloadAccessTokens = archiveRecord.PrimaryDataDownloadAccessTokens;

            if (downloadAccessTokens.Contains(AccessRoles.RoleOe2) || downloadAccessTokens.Exists(t => t.StartsWith("FG_")))
            {
                return CacheRetentionCategory.UsageCopyPublic;
            }

            if (downloadAccessTokens.Contains("DDS") && archiveRecord.ProtectionEndDate?.Date.Date < DateTime.Now.Date)
            {
                return CacheRetentionCategory.UsageCopyPublic;
            }

            if (rolePublicClient == AccessRoles.RoleBAR)
            {
                return CacheRetentionCategory.UsageCopyBarOrAS;
            }

            if (int.TryParse(archiveRecord.ArchiveRecordId, out var veId) && await orderDataAccess.HasEinsichtsbewilligung(veId))
            {
                return CacheRetentionCategory.UsageCopyEB;
            }

            if (rolePublicClient == AccessRoles.RoleAS)
            {
                return CacheRetentionCategory.UsageCopyBarOrAS;
            }

            return CacheRetentionCategory.UsageCopyAB;
        }


        private static IRequestClient<T1> CreateRequestClient<T1>(IBus busControl, string relativeUri) where T1 : class
        {
            var client = busControl.CreateRequestClient<T1>(new Uri(busControl.Address, relativeUri), TimeSpan.FromSeconds(10));
            return client;
        }
    }
}