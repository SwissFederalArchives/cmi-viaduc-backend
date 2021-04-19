using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using Newtonsoft.Json;

namespace CMI.Web.Frontend.Controllers
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
            WebHelper.SetClientType(Response, WebHelper.ClientTypeFrontend);

            var baseUrl = WebHelper.AssertTrailingSlash(Request.ApplicationPath);
            if (!Request.Url.AbsolutePath.EndsWith(baseUrl))
            {
                return Redirect(new Uri(baseUrl + Request.Url.Query, UriKind.Relative).ToString());
            }

            var translations = WebHelper.InjectTranslations ? FrontendSettingsViaduc.Instance.GetTranslations(language) : null;
            var settings = WebHelper.InjectSettings ? FrontendSettingsViaduc.Instance.GetSettings() : null;

            var formatting = Formatting.None;
            ViewBag.Translations = translations != null ? JsonConvert.SerializeObject(translations, formatting) : string.Empty;
            ViewBag.Settings = settings != null ? JsonConvert.SerializeObject(settings, formatting) : string.Empty;

            var title = FrontendSettingsViaduc.Instance.GetTranslation(language, "page.title", "Schweizerisches Bundesarchiv BAR");
            ViewBag.StaticContent =
                FrontendHelper.GetStaticIndexContent(Request, language, title, ViewBag.Translations, ViewBag.Settings, ViewBag.ModelData);

            ViewBag.PageTitle = title;
            ViewBag.Language = language;

            return View();
        }
    }
}