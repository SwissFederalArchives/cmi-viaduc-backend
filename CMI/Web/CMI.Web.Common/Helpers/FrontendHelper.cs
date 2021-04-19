using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;

namespace CMI.Web.Common.Helpers
{
    public static class FrontendHelper
    {
        #region Index

        private static readonly string StaticIndexMarker = "<!--static-index-{0} {1} -->" + Environment.NewLine;

        private static readonly string StaticInjectMarkup = Environment.NewLine + "<script type=\"application/json\" id=\"{0}\">" +
                                                            Environment.NewLine + "{1}" + Environment.NewLine + "</script>" + Environment.NewLine;

        private static readonly Dictionary<string, string> staticIndexContent = new Dictionary<string, string>();
        private static readonly Regex hrefAndSrcMatcher = new Regex("((href|src)=\")(?!(http | file)[^\"]+\")");

        public static string GetStaticIndexContent(HttpRequestBase request, string language, string title, string inlineTranslations,
            string inlineSettings, string inlineModelData)
        {
            var indexKey = "static-" + language;
            var content = string.Empty;
            if (!staticIndexContent.ContainsKey(indexKey) || !WebHelper.EnableIndexPageCaching)
            {
                var indexPath = DirectoryHelper.Instance.IndexPagePath ?? string.Empty;
                if (!string.IsNullOrEmpty(indexPath))
                {
                    var filePath = WebHelper.MapPathIfNeeded(indexPath);
                    if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                    {
                        content = File.ReadAllText(filePath);
                        content = content.Replace("<head", string.Format(StaticIndexMarker, language, indexPath) + "<head");

                        // Clientseitiges Lazyloading von Modulen unterstützen
                        // index.HTML wird bei diesem Vorgang nicht neu angefordert, daher kann die src des neuen modul-"chunks" nicht processed werden
                        // Aktuell wird nun clientseitig mittels deployUrl: 'client/' statisch auf den Clientordner verwiesen

                        // var clientRoot = WebHelper.GetClientRoot(request.ApplicationPath);
                        // content = hrefAndSrcMatcher.Replace(content, "$1" + clientRoot);

                        if (!language.Equals(WebHelper.DefaultLanguage))
                        {
                            content = new Regex("<title>(.*?)</title>").Replace(content, $"<title>{title}</title>");
                            content = content.Replace(" lang=\"de\"", $" lang=\"{language}\"");
                        }

                        if (!string.IsNullOrEmpty(inlineTranslations))
                        {
                            content = content.Replace("</body>",
                                string.Format(StaticInjectMarkup, $"viaduc-translations-{language}", inlineTranslations) + "</body>");
                        }

                        if (!string.IsNullOrEmpty(inlineSettings))
                        {
                            content = content.Replace("</body>", string.Format(StaticInjectMarkup, "viaduc-settings", inlineSettings) + "</body>");
                        }

                        if (!string.IsNullOrEmpty(inlineModelData))
                        {
                            content = content.Replace("</body>", string.Format(StaticInjectMarkup, "viaduc-model", inlineModelData) + "</body>");
                        }
                    }
                }

                staticIndexContent[indexKey] = content;
            }

            return staticIndexContent[indexKey];
        }

        #endregion
    }
}