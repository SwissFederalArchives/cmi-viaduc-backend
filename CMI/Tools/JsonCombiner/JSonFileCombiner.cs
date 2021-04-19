using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace CMI.Tools.JsonCombiner
{
    public class JSonFileCombiner
    {
        public JObject LoadJsonFileFromPath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var a = File.ReadAllText(path);
            return JObject.Parse(a);
        }

        public string CombineJsons(JObject masterJObject, JObject sourceJObject, Language lng)
        {
            if (masterJObject.DescendantsAndSelf().Any(s => s.GetType() == typeof(JArray)))
            {
                throw new Exception("master json can not contain JArray");
            }

            if (sourceJObject.DescendantsAndSelf().Any(s => s.GetType() == typeof(JArray)))
            {
                throw new Exception("source json can not contain JArray");
            }

            foreach (var jObject in masterJObject.DescendantsAndSelf().Where(jt => jt.GetType() == typeof(JObject)).Select(jt => (JObject) jt))
            {
                new JObjectMerger(jObject).Merge((JObject) sourceJObject.SelectToken(jObject.Path) ?? JObject.Parse(@"{}"), lng);
            }

            return masterJObject.ToString();
        }
    }
}