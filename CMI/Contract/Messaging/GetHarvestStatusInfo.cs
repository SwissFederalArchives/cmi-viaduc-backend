using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public class GetHarvestStatusInfo
    {
        public QueryDateRangeEnum DateRange { get; set; }
    }

    public class GetHarvestStatusInfoResult
    {
        public HarvestStatusInfo Result { get; set; }
    }
}