using System.IO;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using MassTransit;

namespace CMI.Utilities.Cache.Access
{
    public interface ICacheHelper
    {
        Task<bool> SaveToCache(IBus bus, CacheRetentionCategory retentionCategory, string file);
        Stream GetStreamFromCache(string ftpUrl);
        Task<string> GetFtpUrl(IBus bus, CacheRetentionCategory retentionCategory, string id);

        Task<CacheRetentionCategory> GetRetentionCategory(ElasticArchiveRecord archiveRecord, string rolePublicClient,
            IOrderDataAccess orderDataAccess);
    }
}