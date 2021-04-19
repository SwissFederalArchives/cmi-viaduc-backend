using System.Web;
using System.Web.SessionState;
using CMI.Web.Management.api.Controllers;

namespace CMI.Web.Frontend
{
    public class Global : HttpApplication
    {
        protected void Application_PostAuthorizeRequest()
        {
            if (RequireSessionState())
            {
                HttpContext.Current.SetSessionStateBehavior(SessionStateBehavior.Required);
            }
        }

        private static bool RequireSessionState()
        {
            var localUrl = HttpContext.Current.Request.Url.LocalPath.ToLowerInvariant();
            return localUrl.Contains("api/auth/" + nameof(AuthController.GetIdentity).ToLowerInvariant());
        }
    }
}