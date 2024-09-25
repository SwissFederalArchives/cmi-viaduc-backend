using System.Globalization;
using System.Threading;
using System.Web.Mvc;
using System.Web.Routing;
using CMI.Utilities.Common.Helpers;
using Serilog;

namespace CMI.Web.Frontend.Controllers
{
    public abstract class BaseController : Controller
    {
        private readonly UserCulture userCulture = new UserCulture();
        private string currentLang = "de";

        protected override void Initialize(RequestContext requestContext)
        {
            base.Initialize(requestContext);
            if (Session != null && Session["CurrentCulture"] != null)
            {
                ChangeCulture(Session["CurrentCulture"].ToString());
            }
            else
            {
                ChangeCulture(currentLang);
            }
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var language = Request.RequestContext.RouteData.Values["lang"] as string;
            if (!string.IsNullOrEmpty(language))
            {
                if (language != currentLang)
                {
                    try
                    {
                        var culture = CultureInfo.GetCultureInfo(language);
                        if (culture != null)
                        {
                            ChangeCulture(language);
                        }
                    }
                    catch (CultureNotFoundException ex)
                    {
                        Log.Warning(ex, "culture for language {language} not found", currentLang);
                    }
                }
            }
        }

        private void ChangeCulture(string lang)
        {
            var cultureInfo = userCulture.GetCultureInfoFromLanguage(lang);

            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            if (Session != null)
            {
                Session["CurrentCulture"] = lang;
            }
            currentLang = lang;
        }
    }
}