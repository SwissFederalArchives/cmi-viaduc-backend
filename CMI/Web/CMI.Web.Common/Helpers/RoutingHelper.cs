using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CMI.Utilities.Common.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;
using WebGrease.Css.Extensions;

namespace CMI.Web.Common.Helpers
{
    public static class RoutingHelper
    {
        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> normalizations;
        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> localizations;
        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> components;

        static RoutingHelper()
        {
            normalizations = new ConcurrentDictionary<string, Dictionary<string, string>>();
            localizations = new ConcurrentDictionary<string, Dictionary<string, string>>();

            components = new ConcurrentDictionary<string, Dictionary<string, string>>();

            Initialize();
        }

        public static void Initialize()
        {
            normalizations.Clear();
            localizations.Clear();
            components.Clear();

            var routesJson = string.Empty;
            try
            {
                var routesPath = WebHelper.MapPathIfNeeded(StringHelper.AddToString("~", "/",
                    StringHelper.AddToString(DirectoryHelper.Instance.ClientDefaultPath, "/", "config/routes.def")));
                routesJson = File.Exists(routesPath) ? File.ReadAllText(routesPath) : string.Empty;
                if (!string.IsNullOrEmpty(routesJson))
                {
                    var i = routesJson.IndexOf("defaultRouteChildren", StringComparison.Ordinal);
                    i = routesJson.IndexOf("[", i, StringComparison.Ordinal);
                    var j = routesJson.IndexOf("];", i, StringComparison.Ordinal);

                    if (i > 0 && j > i)
                    {
                        routesJson = routesJson.Substring(i, j + 1 - i);
                        routesJson = routesJson.Replace("'", "\"");
                        routesJson = new Regex(@"(redirectTo|pathMatch|matcher):\s*[^,\r]+,?").Replace(routesJson, string.Empty);
                        routesJson = new Regex(@"(canActivate|resolve):\s*[^}\]]+[}\]],?").Replace(routesJson, string.Empty);
                        routesJson = new Regex(@"(component):\s*([^,\s]+)[^,\S\r]*,?").Replace(routesJson, @"$1: ""$2"",");
                        var routes = JsonConvert.DeserializeObject<JArray>(routesJson);
                        CollectLocalizations(WebHelper.SupportedLanguages, routes);
                    }
                }
                else
                {
                    Log.Information("Could not load routes: {routesPath}", routesPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not load routes: {routesJson}", routesJson);
            }
        }

        private static string TransformPath(string language, string path, bool normalize = false)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            var mapping = normalize ? normalizations : localizations;

            if (!mapping.TryGetValue(language, out var trans))
            {
                return path;
            }

            var parts = path.ToLowerInvariant().Split('/');
            var i = 0;
            while (i < parts.Length && string.IsNullOrEmpty(parts[i]))
            {
                i += 1;
            }

            if (i < parts.Length && WebHelper.SupportedLanguages.Contains(parts[i]))
            {
                parts[i] = normalize ? WebHelper.DefaultLanguage : language;
                i += 1;
            }

            while (i < parts.Length)
            {
                if (trans.ContainsKey(parts[i]))
                {
                    parts[i] = trans[parts[i]];
                }

                i += 1;
            }

            return string.Join("/", parts);
        }

        public static string LocalizePath(string language, string path)
        {
            return TransformPath(language, path);
        }

        public static string NormalizePath(string language, string path)
        {
            return TransformPath(language, path, true);
        }


        #region Utilities


        private static void AddLocalizations(Dictionary<string, string> dictionary, string keys, string values)
        {
            if (string.IsNullOrEmpty(keys) || string.IsNullOrEmpty(values))
            {
                return;
            }

            var ks = keys.Split('/');
            var vs = values.Split('/');

            for (var i = 0; i < ks.Length && i < vs.Length; i += 1)
            {
                var k = ks[i];
                var v = vs[i];
                if (!string.IsNullOrEmpty(k) && k.IndexOf(':') < 0)
                {
                    dictionary[k] = v;
                }
            }
        }

        private static void CollectLanguageLocalizations(string language, JArray routes, string parentPath)
        {
            localizations.TryGetValue(language, out var locs);
            normalizations.TryGetValue(language, out var norms);
            components.TryGetValue(language, out var comps);

            foreach (JObject route in routes.Children())
            {
                var path = JsonHelper.GetTokenValue<string>(route, "path");
                if (!string.IsNullOrEmpty(path))
                {
                    var fullPath = StringHelper.AddToString(parentPath, "/", path);

                    var component = JsonHelper.GetTokenValue<string>(route, "component");
                    if (!string.IsNullOrEmpty(component))
                    {
                        comps[fullPath] = component;
                    }

                    var localize = JsonHelper.GetTokenValue<JObject>(route, "_localize") ?? new JObject();
                    var localized = JsonHelper.GetTokenValue<string>(localize, language) ?? path;
                    AddLocalizations(locs, path, localized);
                    AddLocalizations(norms, localized, path);
                    var children = route["children"] as JArray;
                    if (children != null)
                    {
                        CollectLanguageLocalizations(language, children, fullPath);
                    }
                }
            }
        }

        private static void CollectLocalizations(IList<string> languages, JArray routes)
        {
            languages.ForEach(language =>
            {
                localizations.GetOrAdd(language, new Dictionary<string, string>());
                normalizations.GetOrAdd(language, new Dictionary<string, string>());
                components.GetOrAdd(language, new Dictionary<string, string>());
                CollectLanguageLocalizations(language, routes, $"/{language}");
            });
        }

        #endregion
    }
}