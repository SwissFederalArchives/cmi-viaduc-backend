using CMI.Contract.Common;

namespace CMI.Manager.Index
{
    public interface IElasticLogManager
    {
        GetElasticLogRecordsResult GetElasticLogRecords(LogDataFilter filter);

        void DeleteOldLogIndexes();
    }
}