using CMI.Access.Sql.Viaduc;
using CMI.Web.Common.Helpers;

namespace CMI.Web.Common.api
{
    public abstract class NewsControllerBase : ApiControllerBase
    {
        protected readonly NewsDataAccess newsDataAccess = new NewsDataAccess(WebHelper.Settings["sqlConnectionString"]);
    }
}