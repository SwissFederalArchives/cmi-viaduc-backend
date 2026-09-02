using CMI.Contract.Common;
using System.Threading.Tasks;

namespace CMI.Manager.Index
{
    public interface IElasticLogManager
    {
        Task<GetElasticLogRecordsResult> GetElasticLogRecords(LogDataFilter filter);

        void DeleteOldLogIndexes();
    }
}