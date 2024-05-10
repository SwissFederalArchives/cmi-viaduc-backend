using CMI.Web.Common.api;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http.Controllers;

namespace CMI.Web.Common.Helpers
{
    public static class StaticContentHelper
    {
        #region Markup

        public const string ProcessedMarker = "<!--static-->";

        public static string ProcessStaticMarkupForMvc(AppSettings settings, HttpRequestBase request, string url, string language, string markup)
        {
            if (markup.IndexOf(ProcessedMarker) >= 0)
            {
                return markup;
            }

            var appRoot = request.ApplicationPath;


            markup = RewriteContentRoot(appRoot, markup);

            markup = markup.Replace("<head ", ProcessedMarker + Environment.NewLine + "<head ");
            return markup;
        }

        public static string ProcessStaticMarkupForSpa(AppSettings settings, HttpRequestContext context, string url, string language, string markup)
        {
            if (markup.IndexOf(ProcessedMarker) >= 0)
            {
                return markup;
            }

            var appRoot = context.VirtualPathRoot;
            var relativeUrl = GetRelativeUrl(appRoot, url, true);
            var anchorPrefix = "#" + relativeUrl;

            markup = RewriteContentRoot(appRoot, markup);
            markup = RewriteAnchors(anchorPrefix, markup);

            markup = markup.Replace("<head ", ProcessedMarker + Environment.NewLine + "<head ");
            return markup;
        }

        private static readonly Regex ContentRootHrefAndSrcMatcher = new Regex("((href|src)=[\"'])/");

        public static string RewriteContentRoot(string appRoot, string markup)
        {
            var appPath = WebHelper.AssertTrailingSlash(appRoot);
            return ContentRootHrefAndSrcMatcher.Replace(markup, "$1" + appPath);
        }

        private static readonly Regex AnchorHrefMatcher = new Regex("(href=[\"'])#([^/])");

        public static string RewriteAnchors(string anchorPrefix, string markup)
        {
            return AnchorHrefMatcher.Replace(markup, "$1" + anchorPrefix + "#$2");
        }

        private const string ContentWrapperId = "content-wrapper";

        public static HtmlNode FindStaticContentNode(AppSettings settings, string html)
        {
            if (!string.IsNullOrEmpty(html))
            {
                var currentDoc = new HtmlDocument();
                currentDoc.LoadHtml(html);

                var contentNode = currentDoc.DocumentNode.SelectSingleNode($"//div[@id = '{ContentWrapperId}']");
                if (contentNode != null)
                {
                    return contentNode;
                }
            }

            return null;
        }

        private static string CleanupEditedMarkup(string appRoot, string markup)
        {
            var appPath = WebHelper.AssertTrailingSlash(appRoot).TrimStart('/');
            var staticPath = WebHelper.GetStaticRoot(appRoot).TrimStart('/');
            var staticPathMatcher = new Regex("((href|src)=[\"']/)" + staticPath);
            var appPathMatcher = new Regex("((href|src)=[\"']/)" + appPath);

            var output = staticPathMatcher.Replace(markup, "$1");
            output = appPathMatcher.Replace(output, "$1");

            return output;
        }

        public static HttpResponseMessage UpdateStaticContent(AppSettings settings, HttpRequestContext context, StaticContentUpdateData update)
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest);

            try
            {
                var appRoot = context.VirtualPathRoot;
                var relativeUrl = GetRelativeUrl(appRoot, update.url);

                var updatedHtml = HttpUtility.UrlDecode(update.markup);
                updatedHtml = CleanupEditedMarkup(appRoot, updatedHtml);

                var updatedDoc = new HtmlDocument();
                updatedDoc.LoadHtml(updatedHtml);

                var currentHtml = GetContentMarkupFor(relativeUrl, update.language);
                if (!string.IsNullOrEmpty(currentHtml))
                {
                    var currentDoc = new HtmlDocument();
                    currentDoc.LoadHtml(currentHtml);

                    var currentContentNode = currentDoc.DocumentNode.SelectSingleNode($"//div[@id = '{ContentWrapperId}']");
                    var updatedContentNode = updatedDoc.DocumentNode.SelectSingleNode($"//div[@id = '{ContentWrapperId}']");

                    if (currentContentNode != null && updatedContentNode != null)
                    {
                        var contentFilePath = GetContentFilePath(relativeUrl);
                        var contentBackPath = contentFilePath + $".{DateTime.Now.ToString("yyyyMMddHHmmss")}.bak";
                        File.Copy(contentFilePath, contentBackPath);

                        currentContentNode.InnerHtml = updatedContentNode.InnerHtml;

                        using (var sw = new StreamWriter(contentFilePath, false, Encoding.UTF8))
                        {
                            sw.Write(currentDoc.DocumentNode.InnerHtml);
                            sw.Flush();
                        }

                        response = new HttpResponseMessage(HttpStatusCode.OK);
                    }
                }
                else
                {
                    response = new HttpResponseMessage(HttpStatusCode.NotFound);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error on UpdateContent");

                response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
                var info = new JObject {{"error", ex.HResult}, {"message", ex.Message}};
                response.Content = new JsonContent(info);
            }

            return response;
        }

        #endregion

        #region Utilities

        private static readonly char[] DefaultValueTrimmer = {'\'', '"'};

        public static string GetRelativeUrl(string appRoot, string url, bool removeExtension = false)
        {
            var relativeUrl = url.Replace(WebHelper.GetStaticRoot(appRoot), "/");
            relativeUrl = relativeUrl.Replace(appRoot, "/").Replace("//", "/");
            var i = relativeUrl.LastIndexOf('/');
            var j = relativeUrl.LastIndexOf('.');
            if (j > i && removeExtension)
            {
                relativeUrl = relativeUrl.Substring(0, j);
            }

            return relativeUrl;
        }

        private static string GetContentFilePath(string url)
        {
            if (url.StartsWith("/"))
            {
                url = url.Remove(0, 1);
            }

            if (!url.Contains("."))
            {
                url += ".html";
            }
           
            var contentDirectory = Path.Combine(WebHelper.MapPathIfNeeded(DirectoryHelper.Instance.StaticPagePath));
            return FindFileInContentDirectory(url, contentDirectory);
        }

        public static string GetContentMarkupFor(string url, string language, bool enableNotFound = false)
        {
            var contentPath = GetContentFilePath(url);
            if (!File.Exists(contentPath) && enableNotFound)
            {
                contentPath = WebHelper.MapPathIfNeeded($"{DirectoryHelper.Instance.StaticPagePath}/{language}/404.html");
            }

            return File.Exists(contentPath) ? File.ReadAllText(contentPath) : null;
        }

        public static string GetContent(string url)
        {
            var contentPath = GetContentFilePath(url);
            return File.Exists(contentPath) ? File.ReadAllText(contentPath) : null;
        }

        private static string FindFileInContentDirectory(string url, string contentDirectory)
        {
            var files = Directory.GetFiles(contentDirectory, "*.html", SearchOption.AllDirectories);
            var file = files.FirstOrDefault(f => f.EndsWith(url.Replace("/", @"\")));

            return file;
        }

        #endregion
    }

    public class StaticContentUpdateData
    {
        public string language;
        public string markup;
        public string url;
    }
}