using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public interface IGetHarvestStatusLogInfo
    {
        QueryDateRangeEnum DateRange { get; set; }
        int PageSize { get; set; }
        int PageNumber { get; set; }
    }
}