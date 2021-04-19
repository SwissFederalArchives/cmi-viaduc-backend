using System.Collections.Generic;
using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public class GetArchiveRecordsForPackageResponse
    {
        public IList<ElasticArchiveRecord> Result { get; set; }
    }
}