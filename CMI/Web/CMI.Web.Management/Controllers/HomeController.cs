using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.api.Configuration;
using Newtonsoft.Json;

namespace CMI.Web.Management.Controllers
{
    public class HomeController : BaseController
    {
        // Disabled die Warning "async method lacks await operators
        // Die methode muss als async gekennzeichnet sein.
#pragma warning disable 1998
        public async Task<ActionResult> Index()
        {
            var language = WebHelper.GetClientLanguage(Request);
            WebHelper.SetClientLanguage(Response, language);
            WebHelper.SetClientType(Response, WebHelper.ClientTypeManagement);

            var appPath = Request.ApplicationPath;
            if (Request.Url.LocalPath.Contains("private"))
            {
                appPath = StringHelper.AddToString(appPath, "/", "private");
            }

            var baseUrl = WebHelper.AssertTrailingSlash(appPath);
            if (!Request.Url.AbsolutePath.EndsWith(baseUrl))
            {
                return Redirect(new Uri(baseUrl + Request.Url.Query, UriKind.Relative).ToString());
            }

            var translations = WebHelper.InjectTranslations ? ManagementSettingsViaduc.Instance.GetTranslations(language) : null;
            var settings = WebHelper.InjectSettings ? ManagementSettingsViaduc.Instance.GetSettings() : null;

            var formatting = Formatting.None;
            ViewBag.Translations = translations != null ? JsonConvert.SerializeObject(translations, formatting) : string.Empty;
            ViewBag.Settings = settings != null ? JsonConvert.SerializeObject(settings, formatting) : string.Empty;

            var title = ManagementSettingsViaduc.Instance.GetTranslation(language, "page.title", "Schweizerisches Bundesarchiv BAR");
            ViewBag.StaticContent =
                FrontendHelper.GetStaticIndexContent(Request, language, title, ViewBag.Translations, ViewBag.Settings, ViewBag.ModelData);

            ViewBag.PageTitle = title;
            ViewBag.Language = language;

            return View();
        }
    }
}