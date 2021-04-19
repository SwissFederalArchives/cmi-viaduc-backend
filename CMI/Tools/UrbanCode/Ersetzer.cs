using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CMI.Tools.UrbanCode
{
    internal class Ersetzer
    {
        private readonly Regex regexObj =
            new Regex(@"@@[\w.]+@@", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.IgnorePatternWhitespace);

        private readonly Dictionary<string, string> werteByTag = new Dictionary<string, string>();

        public Ersetzer(string wertedatei)
        {
            var serializer = new JsonSerializer();
            var obj = (JObject) serializer.Deserialize(new JsonTextReader(new StreamReader(wertedatei)));
            foreach (var p in obj.Properties())
            {
                werteByTag[p.Name] = obj[p.Name].ToString();
            }
        }


        public void Ersetze(string dir)
        {
            var extensions = new [] {"*.config", "*.json", "*.xml"};
            var dirInfo = new DirectoryInfo(dir);

            foreach (var extension in extensions)
            {
                foreach (var file in dirInfo.GetFiles(extension, SearchOption.AllDirectories))
                {
                    DateiVerarbeiten(file);
                }
            }
        }

        private void DateiVerarbeiten(FileInfo file)
        {
            var content = File.ReadAllText(file.FullName);

            foreach (var kvp in werteByTag)
            {
                content = content.Replace(kvp.Key, kvp.Value);
            }

            File.WriteAllText(file.FullName, content);

            WarnungenAusgebenBeiNichtErsetztenPlatzhaltern(file, content);
        }

        private void WarnungenAusgebenBeiNichtErsetztenPlatzhaltern(FileInfo file, string content)
        {
            var matches = regexObj.Matches(content);

            if (matches.Count > 0)
            {
                Console.WriteLine($"{matches.Count} fehlende Ersetzungen in der Datei {file.FullName}:");

                foreach (var m in matches)
                {
                    Console.WriteLine(" - " + m);
                }

                Console.WriteLine();
            }
        }

        public void ReplaceTags()
        {
        }
    }
}