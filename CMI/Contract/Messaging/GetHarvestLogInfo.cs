using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public class GetHarvestLogInfo
    {
        public HarvestLogInfoRequest Request { get; set; }
    }

    public class GetHarvestLogInfoResult
    {
        public HarvestLogInfoResult Result { get; set; }
    }
}