using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using CMI.Utilities.Common.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebGrease.Css.Extensions;

namespace CMI.Web.Common.Helpers
{
    public abstract class TranslationHelper
    {
        #region Generation

        protected readonly char[] SourceDelimiter = {';'};

        public string ClientSrcRoot;
        public string FrontendSrcRoot;
        public string CoreSourcePath;

        public string ConfigRoot;

        public string DefaultOutputRoot;
        public string DefaultDataPath;

        public AppInfo Viaduc;

        public OutputHelper Output = new OutputHelper();

        protected List<string> IgnoreSubPaths = new List<string>
        {
            @"\config\",
            @"\shims\"
        };

        protected const string commonKey = "_";
        protected const string keyPartDelimiter = ".";
        protected char[] keyPartDelimiterSplitter = {'.'};
        protected string nl = Environment.NewLine;

        protected virtual void Initialize()
        {
            var clientRoot = WebHelper.MapPathIfNeeded(StringHelper.AddToString("~", "/", DirectoryHelper.Instance.ClientDefaultPath));
            var clientPath = GetFinalPath(clientRoot);
            var i = clientPath.ToLowerInvariant().IndexOf(@"\dist");
            if (i > 0)
            {
                clientPath = Path.Combine(clientPath.Substring(0, i), "src");
            }

            i = clientPath.ToLowerInvariant().IndexOf(@"\src");
            if (i > 0)
            {
                clientPath = Path.Combine(clientPath.Substring(0, i), "src");
            }

            ClientSrcRoot = clientPath;
            CoreSourcePath = clientPath.Replace("src", Path.Combine("node_modules", "@cmi", "viaduc-web-core", "app"));
            FrontendSrcRoot = WebHelper.MapPathIfNeeded("~");

            ConfigRoot = Path.Combine(ClientSrcRoot, @"config");
            DefaultOutputRoot = Path.Combine(ConfigRoot, @"_generated");
            DefaultDataPath = Path.Combine(ConfigRoot, @"");
        }

        public HttpResponseMessage RunGeneration(string language, HttpRequestMessage request)
        {
            Initialize();

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            try
            {
                var queryString = request.GetQueryNameValuePairs().ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
                Func<string, bool> queryContains = key => queryString.ContainsKey(key) && !string.IsNullOrEmpty(queryString[key]);

                var started = DateTime.Now;
                var info = string.Empty;

                var appInfo = Viaduc;

                var defaultFileName = "translations." + WebHelper.DefaultLanguage + ".json";
                var partialFileName = "translations." + language + ".partial.json";
                var fileName = "translations." + language + ".json";

                var sourcePath = queryContains("in") ? queryString["in"] : appInfo.DataPath;
                var sourceFile = StringHelper.AddToString(sourcePath, @"\", fileName);
                var defaultSourceFile = StringHelper.AddToString(sourcePath, @"\", defaultFileName);
                var partialSourceFile = StringHelper.AddToString(sourcePath, @"\", partialFileName);

                var outputPath = queryContains("out") ? queryString["out"] : DefaultOutputRoot;
                var outputFile = StringHelper.AddToString(outputPath, @"\", fileName);

                Output.Write(string.Format("Generating {0} {1}:", appInfo.AppKey, fileName) + nl);
                Output.Write(string.Format("- {0}", JsonConvert.SerializeObject(appInfo, Formatting.Indented)) + nl);
                Output.Write(string.Format("- in {0}", sourceFile) + nl);
                Output.Write(string.Format("- out {0}", outputFile) + nl);
                Output.Write(nl);

                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }

                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }


                var result = new ProcessResult();
                result.language = language;
                if (!File.Exists(sourceFile))
                {
                    Output.Write("File does not exist: " + sourceFile + nl);
                }
                else
                {
                    result.translations = JsonHelper.GetJsonFromFile(sourceFile);
                    if (!result.forDefaultLanguage)
                    {
                        result.defaultTranslations = JsonHelper.GetJsonFromFile(defaultSourceFile);
                    }
                }

                if (File.Exists(partialSourceFile))
                {
                    result.translationsPartial = JsonHelper.GetJsonFromFile(partialSourceFile);
                }
                else
                {
                    Output.Write("File does not exist: " + partialSourceFile + nl);
                }

                var htmlFiles = new List<FileInfo>();
                var tsFiles = new List<FileInfo>();
                var csFiles = new List<FileInfo>();
                appInfo.Sources.ForEach(source =>
                {
                    var dir = new DirectoryInfo(WebHelper.MapPathIfNeeded(source));
                    htmlFiles.AddRange(dir.GetFiles("*.html", SearchOption.AllDirectories));
                    tsFiles.AddRange(dir.GetFiles("*.ts", SearchOption.AllDirectories).Where(fileInfo => !fileInfo.FullName.Contains(".spec")));
                });
                {
                    var dir = new DirectoryInfo(WebHelper.MapPathIfNeeded(FrontendSrcRoot));
                    csFiles.AddRange(dir.GetFiles("*.cs", SearchOption.AllDirectories));
                }


                result.addInfo = s => AddFileInfo(result, s);
                ProcessFiles(result, "html", htmlFiles);
                ProcessFiles(result, "ts", tsFiles);
                ProcessFiles(result, "cs", csFiles);

                result.addInfo = s => AddDataInfo(result, s);
                ProcessDatas(result, appInfo.AppDatas);

                Output.Write(result.output + nl);

                if (result.filesWithInfos.Count > 0)
                {
                    Output.Write(string.Format("{0} files with infos:", result.filesWithInfos.Count) + nl);
                    foreach (var fileWithInfos in result.filesWithInfos)
                    {
                        Output.Write(string.Format("{0}", fileWithInfos.Key) + nl);
                        foreach (var fileInfo in fileWithInfos.Value)
                        {
                            Output.Write(string.Format("- {0}", fileInfo) + nl);
                        }
                    }
                }

                if (result.filesIgnored.Count > 0)
                {
                    Output.Write(string.Format("{0} files ignored:", result.filesIgnored.Count) + nl);
                    foreach (var fileIgnored in result.filesIgnored)
                    {
                        Output.Write(string.Format("- {0}", fileIgnored) + nl);
                    }
                }

                if (result.datasWithInfos.Count > 0)
                {
                    Output.Write(string.Format("{0} datas with infos:", result.datasWithInfos.Count) + nl);
                    foreach (var dataWithInfos in result.datasWithInfos)
                    {
                        Output.Write(string.Format("{0}", dataWithInfos.Key) + nl);
                        foreach (var dataInfo in dataWithInfos.Value)
                        {
                            Output.Write(string.Format("- {0}", dataInfo) + nl);
                        }
                    }
                }

                result.translations = SortPropertiesAlphabetically(result.translations);

                if (result.translations["__generated"] != null)
                {
                    JsonHelper.Remove(result.translations, "__generated");
                }

                var generated = new JObject();
                generated.Add("info", "CMI.Viaduc.Client.generatetranslations");
                generated.Add("date", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                result.translations.AddFirst(new JProperty("__generated", generated));

                var json = JsonConvert.SerializeObject(result.translations, Formatting.Indented);
                File.WriteAllText(outputFile, json, Encoding.UTF8);

                info = "length=" + json.Length;

                Output.Write(nl);

                Output.Write("Elapsed: " + DateTime.Now.Subtract(started).TotalMilliseconds + nl);
                Output.Write("Output: " + info + nl);
            }
            catch (Exception ex)
            {
                Output.Write(ServiceHelper.GetExceptionInfo(ex));
            }

            response.Content = new StringContent(Output.GetOutput());

            return response;
        }

        public abstract class AppData
        {
            public string Info { get; set; }

            public abstract void Process(TranslationHelper helper, ProcessResult result);
        }

        public class JsonDataMapping
        {
            public string Select { get; set; }
            public Func<JObject, IDictionary<string, string>> Map { get; set; }
        }

        public class JsonAppData : AppData
        {
            public JObject Root { get; set; }
            public List<JsonDataMapping> Mappings { get; set; }

            public override void Process(TranslationHelper helper, ProcessResult result)
            {
                foreach (var mapping in Mappings)
                {
                    try
                    {
                        var found = Root.SelectTokens(mapping.Select);
                        var nodes = found.Where(t => t.Type == JTokenType.Object).Cast<JObject>().ToList();
                        foreach (var node in nodes)
                        {
                            var mapped = mapping.Map(node);

                            foreach (var entry in mapped)
                            {
                                try
                                {
                                    helper.AddOrUpdateEntry(result, entry.Key, entry.Value);
                                }
                                catch (Exception ex)
                                {
                                    result.addInfo(string.Format("error for {0} at {1}: {2}", entry.Key, entry.Value, ex.Message));
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.addInfo(string.Format("error while mapping {0}: {1}", mapping.Select, ex.Message));
                    }
                }
            }
        }


        public class AppInfo
        {
            public AppInfo(TranslationHelper helper, string appKey, string shortKey, string includes = null)
            {
                AppKey = appKey;
                ShortKey = shortKey;
                DataPath = helper.ConfigRoot;
                Sources = new List<string>();
                Sources.Add(helper.CoreSourcePath);
                if (includes != null)
                {
                    includes.Split(';').ForEach(include => { Sources.Add(Path.Combine(helper.ClientSrcRoot, include)); });
                }
            }

            public string AppKey { get; set; }
            public string ShortKey { get; set; }
            public string DataPath { get; set; }
            public List<string> Sources { get; set; }

            [JsonIgnore] public List<AppData> AppDatas { get; set; }

            [JsonProperty("datas")]
            public List<string> DatasInfos
            {
                get { return AppDatas.Select(appData => appData.Info).ToList(); }
            }
        }

        public class ProcessResult
        {
            public Action<string> addInfo;

            public string currentInfo;

            public Dictionary<string, List<string>> datasWithInfos = new Dictionary<string, List<string>>();

            public JObject defaultTranslations = new JObject();
            public List<string> filesIgnored = new List<string>();

            public Dictionary<string, List<string>> filesWithInfos = new Dictionary<string, List<string>>();
            public string language;

            public StringBuilder output = new StringBuilder();
            public JObject translations = new JObject();
            public JObject translationsPartial = new JObject();

            public bool forDefaultLanguage => WebHelper.DefaultLanguage.Equals(language);
        }

        protected void ProcessFiles(ProcessResult result, string info, List<FileInfo> files)
        {
            result.output.AppendLine(info + ": " + files.Count);

            foreach (var file in files)
            {
                result.currentInfo = file.FullName;

                var filePath = file.FullName.ToLower();
                try
                {
                    var ignore = false;
                    if (!ignore)
                    {
                        foreach (var subPath in IgnoreSubPaths)
                        {
                            ignore = ignore || filePath.Contains(subPath);
                        }
                    }

                    if (!ignore)
                    {
                        var source = File.ReadAllText(file.FullName);
                        ProcessFile(result, source);
                    }
                    else if (!filePath.Contains(@"\node") && !filePath.Contains(@"\bin\") && !filePath.Contains(@"\obj\"))
                    {
                        AddFileIgnore(result);
                    }
                }
                catch (Exception ex)
                {
                    result.addInfo(ServiceHelper.GetExceptionInfo(ex));
                }
            }
        }

        protected void ProcessFile(ProcessResult result, string source)
        {
            // html
            {
                var translatePipe = new Regex(@"\{\{(?<text>[^|{]+)\|\s*translate(\:(?<key>[^:}]*)|\s*)\}\}", RegexOptions.Multiline);
                var match = translatePipe.Match(source);
                while (match.Success)
                {
                    var pos = match.Index;
                    var key = match.Groups["key"].Value.Replace(":", string.Empty).Trim().Trim('\'');
                    var text = match.Groups["text"].Value.Trim().Trim('\'');

                    var i = source.LastIndexOf("<", pos - 1, StringComparison.Ordinal);
                    var j = i >= 0 ? source.IndexOf(" ", i, StringComparison.Ordinal) : i;
                    var tag = j > i ? source.Substring(i + 1, j - i - 1) : string.Empty;

                    if (string.IsNullOrEmpty(key) && text.Equals(StringHelper.ToIdentifier(text)))
                    {
                        key = StringHelper.ToIdentifier(text);
                    }

                    if (!string.IsNullOrEmpty(tag) && !string.IsNullOrEmpty(key))
                    {
                        try
                        {
                            AddOrUpdateEntry(result, key, text, pos);
                        }
                        catch (Exception ex)
                        {
                            result.addInfo(string.Format("error for {0} at {1}: {2}", key, pos, ex.Message));
                        }
                    }
                    else
                    {
                        result.addInfo(string.Format("missing key or tag at {1}: key={0}, tag={2}", key, pos, tag));
                    }


                    match = match.NextMatch();
                }
            }

            // js - translationService oder txt
            {
                var txtServiceGet = new Regex(@"(translationService|txt)\.get\(", RegexOptions.Compiled);
                var match = txtServiceGet.Match(source);
                result = FindeTranslationsAndAddToResult(result, source, match);
            }
            // js - translate
            {
                var txtTranslationServiceGet = new Regex(@"(translationService|txt)\.translate\(", RegexOptions.Compiled);
                var match = txtTranslationServiceGet.Match(source);
                result = FindeTranslationsAndAddToResult(result, source, match, TranslationReihenfolge.TextKey);
            }
            // cs
            {
                var translationHelperGet = new Regex(@"GetTranslation\([^,]*,", RegexOptions.Compiled);
                var match = translationHelperGet.Match(source);
                result = FindeTranslationsAndAddToResult(result, source, match);
            }
        }

        private ProcessResult FindeTranslationsAndAddToResult(ProcessResult result, string source, Match match,
            TranslationReihenfolge reihenfolge = TranslationReihenfolge.KeyText)
        {
            while (match.Success)
            {
                var pos = match.Index + match.Length;
                var i = SkipWhiteSpace(source, pos);
                var close = source.Substring(i, 1);
                if (!"\"'".Contains(close))
                {
                    close = ")";
                }
                else
                {
                    i += 1;
                }

                var j = source.IndexOf(close, i, StringComparison.Ordinal);
                if (j > i)
                {
                    var key = source.Substring(i, j - i);
                    if ("\"'".Contains(close))
                    {
                        string text = null;
                        i = SkipWhiteSpace(source, j + 1);
                        if (source.Substring(i, 1).Equals(","))
                        {
                            i = SkipWhiteSpace(source, i + 1);
                        }

                        if (!source.Substring(i, 1).Equals(close))
                        {
                            // no default value
                            text = string.Empty;
                        }
                        else
                        {
                            i += 1;
                            j = i > 0 ? source.IndexOf(close, i, StringComparison.Ordinal) : i;
                            if (i >= 0 && j > i)
                            {
                                text = source.Substring(i, j - i);
                            }
                        }

                        if (text != null)
                        {
                            try
                            {
                                switch (reihenfolge)
                                {
                                    case TranslationReihenfolge.KeyText:
                                        AddOrUpdateEntry(result, key, text, pos);
                                        break;

                                    case TranslationReihenfolge.TextKey:
                                        AddOrUpdateEntry(result, text, key, pos);
                                        break;

                                    default:
                                        throw new ArgumentOutOfRangeException(nameof(reihenfolge), reihenfolge, null);
                                }
                            }
                            catch (Exception ex)
                            {
                                result.addInfo($"error for {key} at {pos}: {ex.Message}");
                            }
                        }
                        else
                        {
                            result.addInfo($"no or bad formatted value for {key} at {pos}");
                        }
                    }
                    else
                    {
                        result.addInfo($"cannot handle dynamic keys for {key} at {pos}");
                    }
                }

                match = match.NextMatch();
            }

            return result;
        }

        private enum TranslationReihenfolge
        {
            KeyText = 10,
            TextKey = 20
        }

        protected void ProcessDatas(ProcessResult result, IList<AppData> datas)
        {
            result.output.AppendLine("datas: " + datas.Count);
            foreach (var data in datas)
            {
                result.currentInfo = data.Info;
                data.Process(this, result);
            }
        }

        protected object FindByKey(JObject translations, string key, out JToken parent, out string parentKey, out string subKey)
        {
            var ks = key.Split(keyPartDelimiterSplitter).ToList();
            object o = null;
            parent = translations;
            parentKey = string.Empty;
            subKey = key;

            // search in common
            if (ks.Count == 1)
            {
                // search in common
                o = JsonHelper.GetTokenValue<object>(translations[commonKey], ks[0], true);
                parent = translations[commonKey];
                subKey = ks[0];
            }

            // search by name space
            while (o == null && parent != null && ks.Count > 0)
            {
                var k = ks[0];
                parentKey = StringHelper.AddToString(parentKey, keyPartDelimiter, k);
                ks.RemoveAt(0);
                parent = JsonHelper.GetTokenValue<JToken>(parent, k, true);
                subKey = string.Join(keyPartDelimiter, ks);
                o = parent != null ? JsonHelper.GetTokenValue<object>(parent, subKey, true) : null;
            }

            if (o == null)
            {
                parentKey = string.Empty;
            }

            // search by full (multi-part) key
            if (o == null && key.Contains(keyPartDelimiter))
            {
                o = JsonHelper.GetTokenValue<object>(translations, key, true);
                parent = translations;
                subKey = key;
            }

            return o;
        }

        private bool ExistsKey(JObject translations, string key)
        {
            var ks = key.Split(keyPartDelimiterSplitter).ToList();
            object o = null;
            JToken parent = translations;
            var parentKey = string.Empty;
            while (o == null && parent != null && ks.Count > 0)
            {
                var k = ks[0];
                parentKey = StringHelper.AddToString(parentKey, keyPartDelimiter, k);
                ks.RemoveAt(0);
                parent = JsonHelper.GetTokenValue<JToken>(parent, k, true);
                var subKey = string.Join(keyPartDelimiter, ks);
                o = parent != null ? JsonHelper.GetTokenValue<object>(parent, subKey, true) : null;
            }

            return o != null;
        }

        protected object AddByKey(JObject translations, string key, out JToken parent, out string parentKey, out string subKey)
        {
            var ks = key.Split(keyPartDelimiterSplitter).ToList();
            object o = null;
            parent = translations;
            parentKey = string.Empty;
            subKey = key;

            while (o == null && parent != null && ks.Count > 1)
            {
                var k = ks[0];
                parentKey = StringHelper.AddToString(parentKey, keyPartDelimiter, k);
                ks.RemoveAt(0);
                var node = JsonHelper.GetTokenValue<JObject>(parent, k, true);
                if (node == null)
                {
                    node = new JObject();
                    (parent as JObject).Add(k.ToLowerCamelCase(), node);
                }

                parent = node;
                subKey = string.Join(keyPartDelimiter, ks);
                o = parent != null ? JsonHelper.GetTokenValue<object>(parent, subKey, true) : null;
            }

            if (o == null)
            {
                parentKey = string.Empty;
            }

            return o;
        }

        public void AddOrUpdateEntry(ProcessResult result, string key, string text, int pos = 0)
        {
            var keyNormalized = StringHelper.GetNormalizedKey(key);
            if (!key.Equals(keyNormalized))
            {
                result.addInfo(
                    pos > 0
                        ? string.Format("normalizing key {0} to {1} at {2}", key, keyNormalized, pos)
                        : string.Format("normalizing key {0} to {1}", key, keyNormalized)
                );
            }

            var translations = result.translations;
            if (translations[commonKey] == null)
            {
                translations.Add(commonKey, new JObject());
            }

            if (string.IsNullOrEmpty(key) || key.EndsWith(keyPartDelimiter))
            {
                result.addInfo($"cannot add node «{keyNormalized}»");
                return;
            }

            JToken parent = translations;
            string parentKey, subKey;
            var o = FindByKey(translations, key, out parent, out parentKey, out subKey);

            if (o == null)
            {
                if (!key.Contains(keyPartDelimiter))
                {
                    parent = translations[commonKey];
                    subKey = key;
                }
                else
                {
                    o = AddByKey(translations, key, out parent, out parentKey, out subKey);
                }
            }

            if (parent is JObject)
            {
                // Alle Zeilenumbrüche eliminieren im Text
                if (text?.Contains(Environment.NewLine) ?? false)
                {
                    text = GetStringWithoutLineBreaks(text);
                }

                var old = o?.ToString();
                if (string.IsNullOrEmpty(old) || !text.Equals(old))
                {
                    if (string.IsNullOrEmpty(subKey))
                    {
                        result.addInfo(string.Format("cannot add node «{0}» at {1}, text='{2}'", keyNormalized, parent.Path, text));
                    }
                    else if (!string.IsNullOrEmpty(old))
                    {
                        if (!string.IsNullOrEmpty(text) && !text.Equals(old) && !key.Equals(old))
                        {
                            result.addInfo($"wont override «{keyNormalized}»: old='{old}', new='{text}'");
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(text) && !result.forDefaultLanguage)
                        {
                            o = FindByKey(result.defaultTranslations, key, out parent, out parentKey, out subKey);
                            if (o != null)
                            {
                                text = o.ToString();
                            }
                        }

                        if (string.IsNullOrEmpty(text))
                        {
                            result.addInfo(string.Format("empty text for «{0}»", keyNormalized));
                        }
                        else if (!result.forDefaultLanguage)
                        {
                            text = result.language + ":" + text;
                            result.addInfo(string.Format("added default text for «{0}»: {1}", keyNormalized, text));
                        }

                        var subKeyNLC = StringHelper.GetNormalizedKey(subKey).ToLowerCamelCase();
                        JsonHelper.AddOrSet(parent as JObject, subKeyNLC, text);
                    }
                }
                else
                {
                    var keyNormaliedLowerCamelCase = keyNormalized.ToLowerCamelCase();
                    var keyC = StringHelper.AddToString(parentKey, keyPartDelimiter, subKey);
                    if (!keyNormaliedLowerCamelCase.Equals(keyC))
                    {
                        result.addInfo(string.Format("existing entry for «{0}» with equal text='{1}' but non-normalized key «{2}»",
                            keyNormaliedLowerCamelCase, text, keyC));
                    }
                }
            }
            else
            {
                result.addInfo(string.Format("no parent for «{0}», text='{1}'", keyNormalized, text));
            }
        }

        protected void AddFileInfo(ProcessResult result, string error, string info = null)
        {
            info = info ?? result.currentInfo;
            if (!result.filesWithInfos.ContainsKey(info))
            {
                result.filesWithInfos.Add(info, new List<string>());
            }

            result.filesWithInfos[info].Add(error);
        }

        protected void AddFileIgnore(ProcessResult result, string info = null)
        {
            result.filesIgnored.Add(info ?? result.currentInfo);
        }

        protected void AddDataInfo(ProcessResult result, string error, string info = null)
        {
            info = info ?? result.currentInfo;
            if (!result.datasWithInfos.ContainsKey(info))
            {
                result.datasWithInfos.Add(info, new List<string>());
            }

            result.datasWithInfos[info].Add(error);
        }

        protected static int SkipWhiteSpace(string source, int pos)
        {
            while (pos < source.Length && string.IsNullOrWhiteSpace(source.Substring(pos, 1)))
            {
                pos += 1;
            }

            return pos;
        }

        protected static JObject SortPropertiesAlphabetically(JObject original)
        {
            var result = new JObject();

            foreach (var property in original.Properties().ToList().OrderBy(p => p.Name))
            {
                var value = property.Value as JObject;

                if (value != null)
                {
                    value = SortPropertiesAlphabetically(value);
                    result.Add(property.Name, value);
                }
                else
                {
                    result.Add(property.Name, property.Value);
                }
            }

            return result;
        }


        public class OutputHelper
        {
            private readonly StringBuilder output = new StringBuilder();

            public void Write(string s)
            {
                output.Append(s);
            }

            public string GetOutput()
            {
                return output.ToString();
            }
        }

        private static string GetFinalPath(string path)
        {
            var fileInfo = new FileInfo(path);
            if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                path = NativeMethods.GetFinalPathName(path);
            }

            return path;
        }

        internal static class NativeMethods
        {
            private const uint FILE_READ_EA = 0x0008;
            private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x2000000;
            private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);

            [DllImport("Kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            private static extern uint GetFinalPathNameByHandle(IntPtr hFile, [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszFilePath,
                uint cchFilePath, uint dwFlags);

            [DllImport("kernel32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr CreateFile(
                [MarshalAs(UnmanagedType.LPTStr)] string filename,
                [MarshalAs(UnmanagedType.U4)] uint access,
                [MarshalAs(UnmanagedType.U4)] FileShare share,
                IntPtr securityAttributes, // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
                [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
                [MarshalAs(UnmanagedType.U4)] uint flagsAndAttributes,
                IntPtr templateFile);

            public static string GetFinalPathName(string path)
            {
                uint maxSize = 4096;
                var h = CreateFile(path, FILE_READ_EA, FileShare.ReadWrite | FileShare.Delete, IntPtr.Zero, FileMode.Open, FILE_FLAG_BACKUP_SEMANTICS,
                    IntPtr.Zero);
                if (h == INVALID_HANDLE_VALUE)
                {
                    throw new Win32Exception();
                }

                try
                {
                    var sb = new StringBuilder((int) maxSize);
                    var res = GetFinalPathNameByHandle(h, sb, maxSize, 0);
                    if (res == 0)
                    {
                        throw new Win32Exception();
                    }

                    var s = sb.ToString();
                    if (s.StartsWith(@"\\?\"))
                    {
                        s = s.Substring(@"\\?\".Length);
                    }

                    return s;
                }
                finally
                {
                    CloseHandle(h);
                }
            }
        }

        private string GetStringWithoutLineBreaks(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }


            var regex = new Regex("(\r\n){1}(\t)*");
            var matchRowBreak = regex.Match(text);
            var list = new List<string>();
            while (matchRowBreak.Success)
            {
                list.Add(matchRowBreak.Value);
                matchRowBreak = matchRowBreak.NextMatch();
            }

            list = list.OrderByDescending(l => l.Length).Distinct().ToList();

            var result = text;
            foreach (var replaceValue in list)
            {
                result = result.Replace(replaceValue, " ");
            }

            return result;
        }

        #endregion
    }
}