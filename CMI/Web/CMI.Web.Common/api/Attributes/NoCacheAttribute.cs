using System.Net.Http.Headers;
using System.Web.Http.Filters;

namespace CMI.Web.Common.api.Attributes
{
    public class NoCacheAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(HttpActionExecutedContext context)
        {
            if (context.Response != null)
            {
                context.Response.Headers.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true,
                    MustRevalidate = true
                };

                context.Response.Headers.Add("Pragma", "no-cache");
            }

            base.OnActionExecuted(context);
        }
    }
}