using CMI.Contract.Common;
using CMI.Contract.Messaging;
using System.Threading.Tasks;

namespace CMI.Manager.DataFeed
{
    public interface IDataFeedManager
    {
       Task HandleActaProSyncRecordAsync(ActaProSyncRecord syncRecord);       
    }
}