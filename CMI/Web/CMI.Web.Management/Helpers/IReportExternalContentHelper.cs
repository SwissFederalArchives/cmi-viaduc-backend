using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Common;

namespace CMI.Web.Management.Helpers
{
    public interface IReportExternalContentHelper
    {
        Task<List<SyncInfoForReport>> GetSyncInfoForReport(int[] mutationsIds);
    }
}
