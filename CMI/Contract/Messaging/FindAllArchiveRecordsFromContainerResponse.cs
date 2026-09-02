using CMI.Contract.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMI.Contract.Messaging
{
    public class FindAllArchiveRecordsFromContainerResponse
    {
        public List<ElasticArchiveRecord> ElasticArchiveRecords { get; set; }
    }
}
