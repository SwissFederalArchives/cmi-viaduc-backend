using CMI.Contract.Harvest;

namespace CMI.Contract.Messaging
{
    public interface IResyncArchiveDatabase
    {
        ResyncRequestInfo RequestInfo { get; set; }
    }
}