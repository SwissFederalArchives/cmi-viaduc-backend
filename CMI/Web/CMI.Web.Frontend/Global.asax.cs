using System;
using System.Web;
using System.Web.Mvc;
using System.Web.SessionState;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Controllers;
using Serilog;

namespace CMI.Web.Frontend
{
    public class Global : HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            MvcHandler.DisableMvcResponseHeader = true;
        }

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
            return localUrl.Contains("api/auth/" + nameof(AuthController.GetIdentity).ToLowerInvariant()) ||
                   localUrl.Contains("api/static/" + nameof(StaticController.UpdateContent).ToLowerInvariant());
        }

        private void Application_Error(object sender, EventArgs e)
        {
            var ex = Server.GetLastError();
            var id = Guid.NewGuid().ToString("N");
            Log.Error(ex, "Unhandled Web-Exception {EXCEPTIONID} for URL {URL}", id, Request.Url);

            var lang = WebHelper.GetClientLanguage(Request);
            var root = Request.Url.GetLeftPart(UriPartial.Authority) + Request.ApplicationPath;

            Response.TrySkipIisCustomErrors = true;
            Server.ClearError();

            if (ex is HttpException exception)
            {
                Response.Redirect(exception.GetHttpCode() == 404
                    ? $"{root}/{lang}/Error/NotFound?url={Request.Url}"
                    : $"{root}/{lang}/Error?exId={id}&url={Request.Url}");
            }
            else
            {
                Response.Redirect($"{root}/{lang}/Error?exId={id}&url={Request.Url}");
            }
        }
    }
}