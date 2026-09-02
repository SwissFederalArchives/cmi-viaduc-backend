using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Common;

namespace CMI.Access.Common
{
    public interface ILogDataAccess
    {
        Task<IList<ElasticRawLogRecord>> GetLogData(LogDataFilter filter);

        void DeleteLogIndexes(DateTime olderThanDate);
    }
}