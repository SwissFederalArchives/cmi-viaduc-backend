using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.Helpers;
using Serilog;

namespace CMI.Web.Frontend.api.Controllers
{
    [AllowAnonymous]
    [EnableCors("*", "*", "*")]
    public class StaticController : ApiFrontendControllerBase
    {
        [HttpGet]
        public HttpResponseMessage GetContent(string url, string language = null)
        {
            language = language ?? WebHelper.GetLanguageFromRequestUrl(url) ?? WebHelper.GetClientLanguage(Request);

            // prevent XSS
            url = HttpUtility.HtmlEncode(url.Trim());
            language = HttpUtility.HtmlEncode(language);

            var statusCode = HttpStatusCode.OK;
            var contentHtml = string.Empty;

            var relativeUrl = url;

            try
            {
                var html = "";

                if (!IsForbiddenUrl(relativeUrl) && WebHelper.SupportedLanguages.Contains(language?.ToLower()))
                {
                    html = StaticContentHelper.GetContentMarkupFor(relativeUrl, language);
                }
                else
                {
                    Log.Warning("User tried to enter a forbidden URL {URL} with LANGUAGE {LANGUAGE}", relativeUrl, language);
                }

                if (string.IsNullOrEmpty(html))
                {
                    html = StaticContentHelper.GetContentMarkupFor($"{language}/404.html", language);
                    relativeUrl = $"{language}/404.html";
                    statusCode = HttpStatusCode.NotFound;
                }

                var contentNode = StaticContentHelper.FindStaticContentNode(FrontendSettingsViaduc.Instance, html);
                contentHtml = StaticContentHelper.ProcessStaticMarkupForSpa(FrontendSettingsViaduc.Instance, RequestContext, relativeUrl, language,
                    contentNode.OuterHtml);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "StaticController.GetContent failed for {url}/{language}", url, language);
                var html = StaticContentHelper.GetContentMarkupFor($"{language}/500.html", language);
                relativeUrl = $"{language}/500.html";

                if (string.IsNullOrEmpty(html))
                {
                    statusCode = HttpStatusCode.InternalServerError;
                }
                else
                {
                    var contentNode = StaticContentHelper.FindStaticContentNode(FrontendSettingsViaduc.Instance, html);
                    contentHtml = StaticContentHelper.ProcessStaticMarkupForSpa(FrontendSettingsViaduc.Instance, RequestContext, relativeUrl,
                        language, contentNode.OuterHtml);
                }
            }

            return CreateResponse(statusCode, contentHtml, "text/html");
        }

        [HttpGet]
        public HttpResponseMessage GetFile(string url)
        {
            var contentPathAdjuster = new Regex(@"(href|src)(=[\""'].*?)(\/?content\/?)");
            var language = WebHelper.GetLanguageFromRequestUrl(url) ?? WebHelper.GetClientLanguage(Request);

            // prevent XSS
            url = HttpUtility.HtmlEncode(url.Trim());

            var statusCode = HttpStatusCode.OK;
            var content = "";

            try
            {
                if (!IsForbiddenUrl(url))
                {
                    content = StaticContentHelper.GetContent(url);
                    content = contentPathAdjuster.Replace(content, "$1$2" + DirectoryHelper.Instance.StaticDefaultPath);
                }
                else
                {
                    Log.Warning("User tried to enter a forbidden URL {URL} with LANGUAGE {LANGUAGE}", url, language);
                }

                if (string.IsNullOrEmpty(content))
                {
                    return CreateResponse(HttpStatusCode.NotFound, "", MimeMapping.GetMimeMapping(url));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "StaticController.GetResource failed for {url}/{language}", url, language);
                var html = StaticContentHelper.GetContentMarkupFor($"{language}/500.html", language);
                url = $"{language}/500.html";

                statusCode = HttpStatusCode.InternalServerError;
                if (!string.IsNullOrEmpty(html))
                {
                    var contentNode = StaticContentHelper.FindStaticContentNode(FrontendSettingsViaduc.Instance, html);
                    content = StaticContentHelper.ProcessStaticMarkupForSpa(FrontendSettingsViaduc.Instance, RequestContext, url, language,
                        contentNode.OuterHtml);
                }
            }

            return CreateResponse(statusCode, content, MimeMapping.GetMimeMapping(url));
        }

        private HttpResponseMessage CreateResponse(HttpStatusCode statusCode, string content, string mediaType)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);

            return response;
        }

        private static bool IsForbiddenUrl(string relativeUrl)
        {
            return relativeUrl.Contains("../") // user could break out, could grab APP_DATA files
                   || relativeUrl.Contains("://") // it is not a relative url
                   || relativeUrl.IndexOf(@"//", StringComparison.Ordinal) == 0;
        }

        [HttpPost]
        public HttpResponseMessage UpdateContent([FromBody] StaticContentUpdateData update)
        {
            if (HttpContext.Current?.Session == null || !HttpContext.Current.Session.HasFeatureStaticContentEdit())
            {
                return new HttpResponseMessage(HttpStatusCode.Forbidden);
            }

            return StaticContentHelper.UpdateStaticContent(FrontendSettingsViaduc.Instance, ControllerContext.RequestContext, update);
        }

        [HttpGet]
        public HttpResponseMessage GenerateTranslations(string language = null)
        {
            return new FrontendTranslationHelper().RunGeneration(language, Request);
        }
    }
}