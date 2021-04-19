using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace CMI.Tools.JsonCombiner
{
    public class JObjectMerger
    {
        private readonly JObject jObject;

        public JObjectMerger(JObject jObject)
        {
            this.jObject = jObject ?? throw new ArgumentNullException(nameof(jObject));
        }

        private IEnumerable<KeyValuePair<string, string>> GetProptyKeyValuePairs()
        {
            return jObject.Properties()
                .Where(jp => jp.Value.GetType() == typeof(JValue))
                .Select(jProperty => new KeyValuePair<string, string>(jProperty.Name, (string) jProperty.Value));
        }

        public void Merge(JObject overrider, Language lng)
        {
            if (overrider == null)
            {
                throw new ArgumentNullException(nameof(overrider));
            }

            foreach (var proptieKeyValuePair in GetProptyKeyValuePairs())
            {
                if (!proptieKeyValuePair.Value.StartsWith(LanguageUtil.LanguageToString(lng)))
                {
                    jObject[proptieKeyValuePair.Key] = $"{LanguageUtil.LanguageToString(lng)} {proptieKeyValuePair.Value}";
                }
            }

            foreach (var proptieKeyValuePair in new JObjectMerger(overrider).GetProptyKeyValuePairs())
            {
                jObject[proptieKeyValuePair.Key] = proptieKeyValuePair.Value;
            }
        }
    }
}