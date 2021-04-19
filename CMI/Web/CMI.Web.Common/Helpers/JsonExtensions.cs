using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Common.Helpers
{
    public static class JsonExtensions
    {
        public static void ForAllEntries<T>(this JToken itemValue, Action<T> doTo, bool treatObjectsAsArray = false) where T : JToken
        {
            if (itemValue is JArray || treatObjectsAsArray && itemValue is JObject)
            {
                foreach (var item in itemValue.Children())
                {
                    if (item is T)
                    {
                        doTo(item as T);
                    }
                }
            }
            else if (itemValue as T != null)
            {
                doTo(itemValue as T);
            }
        }

        public static void ForAllEntries(this JToken itemValue, Action<JToken> doTo, bool treatObjectsAsArray = false)
        {
            itemValue.ForAllEntries<JToken>(doTo, treatObjectsAsArray);
        }

        public static void RemoveDescendantsByName(this JToken token, HashSet<string> names)
        {
            var container = token as JContainer;
            if (container == null)
            {
                return;
            }

            var removes = new List<JToken>();
            foreach (var child in container.Children())
            {
                var p = child as JProperty;
                if (p != null && names.Contains(p.Name))
                {
                    removes.Add(child);
                }
            }

            foreach (var remove in removes)
            {
                remove.Remove();
            }

            foreach (var child in container.Children())
            {
                child.RemoveDescendantsByName(names);
            }
        }

        public static void ExtendyBy(this JContainer left, JProperty right)
        {
            var leftProperty = left[right.Name];

            if (leftProperty == null)
            {
                left.Add(right);
            }
            else
            {
                var leftObject = leftProperty as JObject;
                if (leftObject == null)
                {
                    var leftParent = (JProperty) leftProperty.Parent;
                    leftParent.Value = right.Value;
                }
                else
                {
                    ExtendBy(leftObject, right.Value);
                }
            }
        }

        public static void ExtendBy(this JContainer left, JToken right)
        {
            foreach (var rightChild in right.Children<JProperty>())
            {
                ExtendBy(left, rightChild);
            }
        }

        public static JToken Merge(this JToken left, JToken right)
        {
            if (left.Type != JTokenType.Object)
            {
                return right.DeepClone();
            }

            var leftClone = (JContainer) left.DeepClone();
            ExtendBy(leftClone, right);
            return leftClone;
        }


        public static JToken GetTokenByKey(this JObject obj, string key, bool ignoreCase = false)
        {
            return JsonHelper.GetTokenByKey(obj, key, ignoreCase);
        }

        public static R GetTokenValue<R>(this JToken parent, string key, bool ignoreCase = false)
        {
            return JsonHelper.GetTokenValue<R>(parent, key, ignoreCase);
        }

        public static JToken SelectTokenByPath(this JToken token, string path, bool ignoreCase = false)
        {
            return JsonHelper.SelectTokenByPath(token, path, ignoreCase);
        }
    }
}