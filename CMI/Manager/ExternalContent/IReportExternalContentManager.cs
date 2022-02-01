using System.Collections.Generic;
using CMI.Contract.Common;

namespace CMI.Manager.ExternalContent
{
    public interface IReportExternalContentManager
    {
        SyncInfoForReportResult GetReportExternalContent(int[] mutationsIds);
    }
}
