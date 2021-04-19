using System;
using System.Collections.Generic;
using System.Web.Mvc;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.Models;

namespace CMI.Web.Frontend.Controllers
{
    public class ContentController : BaseController
    {
        public ActionResult Index()
        {
            var language = WebHelper.GetClientLanguage(Request);

            ViewBag.Language = language;
            ViewBag.Title =
                FrontendSettingsViaduc.Instance.GetTranslation(language, "page.title",
                    "Schweizerisches Bundesarchiv BAR");

            var viewModel = new ContentIndexModel();

            var contentHtml = string.Empty;

            var subRequestPath = WebHelper.GetApplicationSubRequestPath(Request);
            var relativeUrl = StaticContentHelper.GetRelativeUrl(Request.ApplicationPath, subRequestPath, true);

            var contentMarkup = string.Empty;
            try
            {
                contentMarkup = StaticContentHelper.GetContentMarkupFor(relativeUrl, language);
                if (string.IsNullOrEmpty(contentMarkup))
                {
                    contentMarkup = StaticContentHelper.GetContentMarkupFor($"{language}/404.html", language);
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                contentMarkup = StaticContentHelper.GetContentMarkupFor($"{language}/500.html", language);
            }

            try
            {
                var contentNode =
                    StaticContentHelper.FindStaticContentNode(FrontendSettingsViaduc.Instance, contentMarkup);

                contentHtml = StaticContentHelper.ProcessStaticMarkupForMvc(FrontendSettingsViaduc.Instance, Request,
                    relativeUrl, language, contentNode.OuterHtml);
            }
            catch (Exception ex)
            {
                contentHtml = "<textarea>" + ServiceHelper.GetExceptionInfo(ex) + "</textarea>";
            }

            ViewBag.Html = contentHtml;

            var editMode = viewModel.EditMode = Session.HasFeatureStaticContentEdit();
            viewModel.BaseUrl = Request.ApplicationPath;

            ViewBag.HtmlClasses = editMode ? "edit-mode" : string.Empty;

            var normalizedUrl = RoutingHelper.NormalizePath(language, relativeUrl);
            viewModel.LanguageLinks = new Dictionary<string, string>();
            WebHelper.SupportedLanguages.ForEach(lang =>
            {
                viewModel.LanguageLinks[lang] = StringHelper.AddToString(Request.ApplicationPath, "/",
                    RoutingHelper.LocalizePath(lang, normalizedUrl));
            });

            return View(viewModel);
        }
    }
}