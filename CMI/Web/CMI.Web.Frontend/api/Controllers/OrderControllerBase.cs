using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;

namespace CMI.Web.Frontend.api.Controllers
{
    public class OrderControllerBase : ApiFrontendControllerBase
    {
        protected bool IsEinsichtsbewilligungNotwendig(ElasticArchiveRecord record, UserAccess access, bool hasBewilligungsDatum)
        {
            return record.HasCustomProperty("zugänglichkeitGemässBga")
                   && record.CustomFields.zugänglichkeitGemässBga == "In Schutzfrist"
                   && !access.HasAnyTokenFor(record.PrimaryDataDownloadAccessTokens)
                   && !hasBewilligungsDatum;
        }
    }
}