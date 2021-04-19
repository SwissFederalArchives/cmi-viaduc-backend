using System;
using System.Collections.Generic;
using CMI.Contract.Common;

namespace CMI.Access.Common
{
    public interface ILogDataAccess
    {
        IList<ElasticRawLogRecord> GetLogData(LogDataFilter filter);

        void DeleteLogIndexes(DateTime olderThanDate);
    }
}