using System.Web.Mvc;
using CMI.Web.Common.Helpers;

namespace CMI.Web.Frontend.Controllers
{
    public class PrivateController : BaseController
    {
        public ActionResult RedirectToSignIn()
        {
            return Redirect(WebHelper.GetStringSetting("erneutAnmeldenlogin", "https://www.recherche.bar.admin.ch/recherche/AuthServices/SignIn"));
        }
    }
}