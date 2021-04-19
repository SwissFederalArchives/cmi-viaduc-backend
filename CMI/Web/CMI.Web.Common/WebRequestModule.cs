using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using CMI.Utilities.Common.Helpers;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;

namespace CMI.Web.Common
{
    public class WebRequestModule : IHttpModule
    {
        public void Dispose()
        {
        }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += context_BeginRequest;
            context.PreRequestHandlerExecute += context_PreRequestHandlerExecute;
            context.EndRequest += context_EndRequest;
        }

        #region Statics

        private const string webAppRootPrefix = "~/";

        public const string RequestRawUrlKey = "cmiRawUrl";

        private static readonly Regex multipleSlashReplace = new Regex("/{1,}");

        private static readonly string[] clientDirectoriesToMap =
        {
            "assets",
            "client",
            "config"
        };

        private static readonly HashSet<string> clientDirectoriesToMapLowerCased;
        private static readonly Regex clientDirectoriesToMapMatcher;

        private static readonly string[] staticDirectoriesToMap =
        {
            "de",
            "en",
            "fr",
            "it",
            "css",
            "fonts",
            "img",
            "js"
        };

        private static readonly HashSet<string> staticDirectoriesToMapLowerCased;
        private static readonly Regex staticDirectoriesToMapMatcher;

        static WebRequestModule()
        {
            string r;

            clientDirectoriesToMapLowerCased = new HashSet<string>();
            r = string.Empty;
            Array.ForEach(clientDirectoriesToMap, s =>
            {
                clientDirectoriesToMapLowerCased.Add(s.ToLower());
                r = StringHelper.AddToString(r, "|", s.ToLower());
            });
            clientDirectoriesToMapMatcher = new Regex(string.Format("/({0})/", r), RegexOptions.IgnoreCase | RegexOptions.Singleline);

            staticDirectoriesToMapLowerCased = new HashSet<string>();
            r = string.Empty;
            Array.ForEach(staticDirectoriesToMap, s =>
            {
                staticDirectoriesToMapLowerCased.Add(s.ToLower());
                r = StringHelper.AddToString(r, "|", s.ToLower());
            });
            staticDirectoriesToMapMatcher = new Regex(string.Format("/({0})/", r), RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        private readonly string hashMarkerPart = WebHelper.LinkHashMarkerPart;
        private readonly string skipMarkerPart = WebHelper.LinkSkipMarkerPart;

        #endregion

        #region Events

        private void context_BeginRequest(object sender, EventArgs e)
        {
            var context = ((HttpApplication) sender).Context;
            var requestUrl = context.Request.Url.PathAndQuery;
            var rewriteUrl = string.Empty;

            if (string.IsNullOrEmpty(rewriteUrl))
            {
                DetectRewriteRequest(context, requestUrl, out rewriteUrl);
                if (!string.IsNullOrEmpty(rewriteUrl))
                {
                    context.Items[RequestRawUrlKey] = requestUrl;
                    context.RewritePath(rewriteUrl);
                }
            }
        }

        private void context_PreRequestHandlerExecute(object sender, EventArgs e)
        {
            var context = ((HttpApplication) sender).Context;

            // Restore the original raw url for browser
            if (context.Items[RequestRawUrlKey] != null)
            {
                context.RewritePath((string) context.Items[RequestRawUrlKey]);
            }
        }

        private void context_EndRequest(object sender, EventArgs e)
        {
            var context = ((HttpApplication) sender).Context;

            context.Items[RequestRawUrlKey] = null;
        }

        #endregion


        #region Private

        private void DetectRewriteRequest(HttpContext context, string requestUrl, out string rewrittenFileUrl)
        {
            rewrittenFileUrl = null;

            var request = context.Request;
            var dirHelper = DirectoryHelper.Instance;

            var method = request.HttpMethod.ToUpper();
            var isGet = "GET".Equals(method);

            if (isGet)
            {
                var webPrefix = request.ApplicationPath;

                var requestPath = request.Path;
                var subRequestPath = WebHelper.GetApplicationSubRequestPath(request);
                if ("/".Equals(subRequestPath))
                {
                    // Ignore
                }
                else
                {
                    var parts = subRequestPath.Split(WebHelper.UrlSplitter, StringSplitOptions.RemoveEmptyEntries);
                    var folderPart = parts.FirstOrDefault();

                    var handled = false;

                    // Static rewrites
                    if (folderPart != null && staticDirectoriesToMapLowerCased.Contains(folderPart.ToLower()) ||
                        staticDirectoriesToMapMatcher.Match(subRequestPath).Success)
                    {
                        var filePath = context.Server.MapPath(requestPath);
                        if (!File.Exists(filePath))
                        {
                            if (!requestPath.Contains(".html"))
                            {
                                var incPath = StringHelper.AddToString(dirHelper.StaticDefaultPath, "/", subRequestPath);
                                filePath = context.Server.MapPath((!incPath.StartsWith(webAppRootPrefix) ? webAppRootPrefix : string.Empty) +
                                                                  incPath);
                                if (File.Exists(filePath))
                                {
                                    rewrittenFileUrl = StringHelper.AddToString(webPrefix, "/", incPath.Replace(webAppRootPrefix, string.Empty));
                                    handled = true;
                                }
                            }
                        }
                        else
                        {
                            handled = true;
                        }
                    }

                    // Client rewrites
                    Match clientMatch = null;
                    if (!handled && (folderPart != null && clientDirectoriesToMapLowerCased.Contains(folderPart.ToLower()) ||
                                     (clientMatch = clientDirectoriesToMapMatcher.Match(subRequestPath)).Success))
                    {
                        handled = true;

                        var filePath = context.Server.MapPath(requestPath);
                        if (!File.Exists(filePath))
                        {
                            var resPath = subRequestPath;
                            if (clientMatch != null)
                            {
                                resPath = resPath.Remove(0, clientMatch.Index);
                            }

                            if (!string.IsNullOrEmpty(dirHelper.ClientDefaultPath) && !"/".Equals(dirHelper.ClientDefaultPath))
                            {
                                resPath = ReplaceFirst(resPath, dirHelper.ClientDefaultPath, "/");
                            }

                            // look in web 
                            if (rewrittenFileUrl == null)
                            {
                                var incPath = StringHelper.AddToString(dirHelper.ClientDefaultPath, "/", resPath);
                                filePath = context.Server.MapPath((!incPath.StartsWith(webAppRootPrefix) ? webAppRootPrefix : string.Empty) +
                                                                  incPath);
                                if (File.Exists(filePath))
                                {
                                    rewrittenFileUrl = StringHelper.AddToString(webPrefix, "/", incPath.Replace(webAppRootPrefix, string.Empty));
                                }
                            }
                        }
                    }

                    // Link handling
                    if (!handled && !requestUrl.Contains("/" + ApiHelper.WebApiSubRoot + "/") && !requestUrl.Contains("/proxy/") &&
                        requestUrl.Contains(hashMarkerPart))
                    {
                        var redirectTo = request.RawUrl.Replace(hashMarkerPart, "/#/");
                        if (!string.IsNullOrEmpty(skipMarkerPart) && redirectTo.Contains(skipMarkerPart))
                        {
                            var i = redirectTo.IndexOf(skipMarkerPart);
                            var j = redirectTo.LastIndexOf("?", i);
                            if (j > 0 && "/".Equals(redirectTo[j - 1]))
                            {
                                j -= 1;
                            }

                            redirectTo = j > i ? redirectTo.Substring(0, i) + redirectTo.Substring(j) : redirectTo.Substring(0, i);
                        }

                        redirectTo = multipleSlashReplace.Replace(redirectTo, "/");

                        context.Response.Clear();
                        context.Response.Redirect(redirectTo);
                        context.Response.End();
                    }
                }
            }
        }

        private string ReplaceFirst(string text, string search, string replace)
        {
            var pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }

            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        #endregion
    }
}