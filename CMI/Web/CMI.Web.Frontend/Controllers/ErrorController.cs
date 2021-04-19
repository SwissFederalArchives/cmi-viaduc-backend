using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Mvc;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.Models;
using Serilog;

namespace CMI.Web.Frontend.Controllers
{
    public class ErrorController : BaseController
    {
        public async Task<ActionResult> Index(string exId, string url = "")
        {
            var language = WebHelper.GetClientLanguage(Request);
            var contentMarkup = StaticContentHelper.GetContentMarkupFor($"{language}/500.html", language);
            var viewModel = CreateErrorViewModel(contentMarkup, language, url);
            viewModel.ErrorId = exId;

            var viewResult = await Task.FromResult(View(viewModel));
            return viewResult;
        }

        public async Task<ActionResult> NotFound(string url = "")
        {
            var language = WebHelper.GetClientLanguage(Request);
            var contentMarkup = StaticContentHelper.GetContentMarkupFor($"{language}/404.html", language);
            var viewModel = CreateErrorViewModel(contentMarkup, language, url, true);

            Log.Warning("Non-Existing Route {ROUTE} was called", viewModel.Url);

            var viewResult = await Task.FromResult(View("Index", viewModel));
            return viewResult;
        }

        private ErrorModel CreateErrorViewModel(string contentMarkup, string language, string url, bool is404 = false)
        {
            ViewBag.Language = language;
            ViewBag.Title = FrontendSettingsViaduc.Instance.GetTranslation(language, "page.title",
                "Schweizerisches Bundesarchiv BAR");

            var viewModel = new ErrorModel();

            var subRequestPath = WebHelper.GetApplicationSubRequestPath(Request);
            var relativeUrl = StaticContentHelper.GetRelativeUrl(Request.ApplicationPath, subRequestPath, true);

            var contentNode =
                StaticContentHelper.FindStaticContentNode(FrontendSettingsViaduc.Instance, contentMarkup);

            var contentHtml = StaticContentHelper.ProcessStaticMarkupForMvc(FrontendSettingsViaduc.Instance, Request,
                relativeUrl, language, contentNode.OuterHtml);

            ViewBag.Html = contentHtml;
            viewModel.BaseUrl = Request.ApplicationPath;
            viewModel.Url = url;

            viewModel.LanguageLinks = new Dictionary<string, string>();
            var errorUrl = is404 ? "error/notfound" : $"error?url={url}";

            WebHelper.SupportedLanguages.ForEach(lang =>
            {
                viewModel.LanguageLinks[lang] = StringHelper.AddToString(Request.ApplicationPath, $"/{lang}/", errorUrl);
            });
            return viewModel;
        }
    }
}