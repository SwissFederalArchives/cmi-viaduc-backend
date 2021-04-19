using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public class GetElasticLogRecordsRequest
    {
        public LogDataFilter DataFilter { get; set; }
    }
}