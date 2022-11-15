using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Web.Optimization;
using CMI.Utilities.Common.Helpers;
using Serilog;

namespace CMI.Web.Common.Helpers
{
    public static class WebHelper
    {
        public const string WebAppRootPrefix = "~/";
        public const string DefaultLanguage = "de";

        public const string ClientTypeFrontend = "frontend";
        public const string ClientTypeManagement = "management";

        public const string CookiePcAspNetSessionIdKey = "V_PC_ASP_SessionId";
        public const string CookieMcAspNetSessionIdKey = "V_MC_ASP_SessionId";
        public const string CookiePcViaducUserIdKey = "V_PC_UserId";
        public const string CookieMcViaducUserIdKey = "V_MC_UserId";
        public const string CookiePcAppliationCookieKey = "V_PC_AppCookie";
        public const string CookieMcAppliationCookieKey = "V_MC_AppCookie";

        public static readonly List<string> SupportedLanguages = new List<string> {"de", "fr", "it", "en"};

        private static readonly Regex LanguageInUrlMatcher = new Regex("^.*/(?<language>(de|fr|it|en))/");

        public static readonly char[] UrlSplitter = {'/'};

        #region Filtering

        public static IList<Func<HttpContext, string, string, string>> StaticContentFilters = new List<Func<HttpContext, string, string, string>>();

        #endregion

        public static string LanguageCookieName => GetStringSetting("languageCookieName", "viaduc_language");
        public static string ClientTypeCookieName => GetStringSetting("clientTypeCookieName", "viaduc_client");

        public static bool EnableIndexPageCaching => GetBooleanSetting("enableIndexPageCaching", true);

        public static bool EnableStaticPagesCaching => GetBooleanSetting("enableStaticPagesCaching", true);

        public static bool EnableTranslationsCaching => GetBooleanSetting("enableTranslationsCaching", true);
        public static bool EnableSettingsCaching => GetBooleanSetting("enableSettingsCaching", true);
        public static bool EnableModelDataCaching => GetBooleanSetting("enableModelDataCaching", true);

        public static bool InjectTranslations => GetBooleanSetting("injectTranslations", true);
        public static bool InjectSettings => GetBooleanSetting("injectSettings");

        public static string SupportedLanguagesForChatBot => GetStringSetting("supportedLanguagesForChatBot", "de");
        public static string UrlForChatBot => GetStringSetting("urlForChatBot", "https://chatbot.bar.smartive.cloud/");

        public static string PublicClientUrl => GetStringSetting("publicClientUrl", "https://www.recherche.bar.admin.ch/recherche");

        public static string SwaggerBaseUrl => GetStringSetting("swaggerBaseUrl", "https://www.recherche.bar.admin.ch/recherche");

        public static string ManagementAuthReturnUrl => GetStringSetting("managementAuthReturnUrl", "http://localhost/management/#/auth/success");

        public static string ManagementLogoutReturnUrl => GetStringSetting("managementLogoutReturnUrl", "http://localhost/management");

        public static string FrontendAuthReturnUrl => GetStringSetting("frontendAuthReturnUrl", "https://www.recherche.bar.admin.ch/recherche/#/auth/success");

        public static string FrontendLogoutReturnUrl => GetStringSetting("frontendLogoutReturnUrl", "https://www.recherche.bar.admin.ch/recherche");

        public static string MatomoUrl => GetStringSetting("matomo-url", "");
        public static int MatomoSiteId => GetIntSetting("matomo-siteId");

        public static string LinkHashMarkerPart => GetStringSetting("linkHashMarkerPart", "/link/");
        public static string LinkSkipMarkerPart => GetStringSetting("linkSkipMarkerPart", "--");

        #region CMI / App Settings

        public static CmiSettings Settings { get; } = new CmiSettings();

        #endregion

        public static string GetClientLanguage(HttpRequest request)
        {
            var language = GetLanguageFromRequestUrl(request.RawUrl.ToLowerInvariant());
            var cookie = language == null ? request?.Cookies.Get(LanguageCookieName) : null;
            if (cookie != null && !string.IsNullOrEmpty(cookie.Value) && SupportedLanguages.Contains(cookie.Value.ToLowerInvariant()))
            {
                language = cookie.Value.ToLowerInvariant();
            }

            if (language == null && request.UserLanguages != null)
            {
                for (var i = 0; i < request.UserLanguages.Length && language == null; i += 1)
                {
                    var lang = (request.UserLanguages[i] ?? "xx").Split('-')[0];
                    if (!string.IsNullOrEmpty(lang) && SupportedLanguages.Contains(lang.ToLowerInvariant()))
                    {
                        language = lang;
                    }
                }
            }

            return language ?? DefaultLanguage;
        }

        public static string GetClientLanguage(HttpRequestBase request)
        {
            var language = GetLanguageFromRequestUrl(request.RawUrl.ToLowerInvariant());
            var cookie = language == null ? request?.Cookies[LanguageCookieName] : null;
            if (cookie != null && !string.IsNullOrEmpty(cookie.Value) && SupportedLanguages.Contains(cookie.Value.ToLowerInvariant()))
            {
                language = cookie.Value.ToLowerInvariant();
            }

            if (language == null && request.UserLanguages != null)
            {
                for (var i = 0; i < request.UserLanguages.Length && language == null; i += 1)
                {
                    var lang = (request.UserLanguages[i] ?? "xx").Split('-')[0];
                    if (!string.IsNullOrEmpty(lang) && SupportedLanguages.Contains(lang.ToLowerInvariant()))
                    {
                        language = lang;
                    }
                }
            }

            return language ?? DefaultLanguage;
        }

        public static string GetClientLanguage(HttpRequestMessage request)
        {
            var language = GetLanguageFromRequestUrl(request.RequestUri.PathAndQuery.ToLowerInvariant());
            var cookie = language == null ? request.Headers.GetCookies(LanguageCookieName).FirstOrDefault() : null;
            var cookieValue = cookie != null ? cookie[LanguageCookieName].Value : null;
            if (!string.IsNullOrEmpty(cookieValue) && SupportedLanguages.Contains(cookieValue.ToLowerInvariant()))
            {
                language = cookieValue.ToLowerInvariant();
            }

            return language ?? DefaultLanguage;
        }

        public static void SetClientLanguage(HttpResponseBase response, string language)
        {
            var languageCookie = new HttpCookie(LanguageCookieName);
            languageCookie.Value = language;
            languageCookie.Expires = DateTime.Now.AddDays(365);
            response.Cookies.Add(languageCookie);
        }

        public static void SetClientType(HttpResponseBase response, string type)
        {
            var clientTypeCookie = new HttpCookie("viaduc_client");
            clientTypeCookie.Value = type;
            response.Cookies.Add(clientTypeCookie);
        }

        public static string GetLanguageFromRequestUrl(string url)
        {
            string language = null;
            var match = LanguageInUrlMatcher.Match(url.ToLowerInvariant());
            if (match.Success && SupportedLanguages.Contains(match.Groups["language"].Value.ToLowerInvariant()))
            {
                language = match.Groups["language"].Value.ToLowerInvariant();
            }

            return language;
        }

        public static string AssertTrailingSlash(string url)
        {
            return url + (!url.EndsWith("/") ? "/" : string.Empty);
        }

        public static string GetClientRoot(string appRoot)
        {
            var clientRoot = StringHelper.AddToString(appRoot, "/", DirectoryHelper.Instance.ClientDefaultPath);
            return AssertTrailingSlash(clientRoot);
        }

        public static string GetStaticRoot(string appRoot)
        {
            var staticRoot = StringHelper.AddToString(appRoot, "/", DirectoryHelper.Instance.StaticDefaultPath);
            return AssertTrailingSlash(staticRoot);
        }

        public static string MapPathIfNeeded(string path)
        {
            if (!string.IsNullOrEmpty(path) && (path.StartsWith("~") || !Directory.Exists(path) && !File.Exists(path)))
            {
                var newPath = "";
                try
                {
                    newPath = HttpContext.Current?.Server?.MapPath(path) ?? HostingEnvironment.MapPath(path);
                }
                catch (Exception e)
                {
                    Log.Error("Error in MapPathIfNeeded {ERROR}", e);
                }
                finally
                {
                    path = newPath;
                }
            }

            return path;
        }

        #region Utilities

        public static string GetStringSetting(string key, string defaultValue = null)
        {
            return Settings[key] ?? defaultValue;
        }

        public static bool GetBooleanSetting(string key, bool defaultValue = false)
        {
            var value = false;
            if (!bool.TryParse(Settings[key], out value))
            {
                value = defaultValue;
            }

            return value;
        }

        public static int GetIntSetting(string key, int defaultValue = default)
        {
            var value = -1;
            if (!int.TryParse(Settings[key], out value))
            {
                value = defaultValue;
            }

            return value;
        }

        #endregion

        #region Requests

        public static string GetApplicationSubRequestPath(HttpRequest request)
        {
            return GetApplicationSubRequestPath(request.ApplicationPath, request.Path);
        }

        public static string GetApplicationSubRequestPath(HttpRequestBase request)
        {
            return GetApplicationSubRequestPath(request.ApplicationPath, request.Path);
        }


        private static string GetApplicationSubRequestPath(string applicationPath, string requestPath)
        {
            string url = null;
            if (requestPath != null)
            {
                url = requestPath;
                if (!string.IsNullOrEmpty(applicationPath) && !"/".Equals(applicationPath))
                {
                    url = url.Substring(applicationPath.Length);
                }
            }

            if (string.IsNullOrEmpty(url))
            {
                url = "/";
            }

            return url;
        }


        public static HttpRequestMessage CloneHttpRequestMessage(this HttpRequestMessage req)
        {
            var clone = new HttpRequestMessage(req.Method, req.RequestUri);

            if (req.Method != HttpMethod.Get)
            {
                clone.Content = req.Content;
            }

            clone.Version = req.Version;

            foreach (var prop in req.Properties)
            {
                clone.Properties.Add(prop);
            }

            foreach (var header in req.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }

        #endregion

        #region Bundles

        public static void SetupClientDefaultBundleConfig(BundleCollection bundles)
        {
            var clientRoot = StringHelper.AddToString("~", "/", DirectoryHelper.Instance.ClientDefaultPath);

            if (!Directory.Exists(MapPathIfNeeded(clientRoot)))
            {
                return;
            }

            BundleTable.EnableOptimizations = false;
        }

        private static void IncludeDirectoryIfPathExists(Bundle bundle, string directoryVirtualPath, string searchPattern,
            bool searchSubdirectories = true)
        {
            if (Directory.Exists(MapPathIfNeeded(directoryVirtualPath)))
            {
                bundle.IncludeDirectory(directoryVirtualPath, searchPattern, searchSubdirectories);
            }
        }

        #endregion
    }
}