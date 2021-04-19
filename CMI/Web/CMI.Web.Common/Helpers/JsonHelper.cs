using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CMI.Web.Common.api;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Common.Helpers
{
    public static class JsonHelper
    {
        public static void AddOrSet(JObject obj, string key, object value, bool ignoreCase = false)
        {
            var prop = obj.Property(key);
            if (prop == null && ignoreCase)
            {
                prop = obj.GetTokenByKey(key, true) as JProperty;
            }

            if (prop == null)
            {
                obj.Add(new JProperty(key, value));
            }
            else
            {
                prop.Value = value as JToken ?? new JValue(value);
            }
        }

        public static void AddFirst(JObject obj, string key, object value, bool ignoreCase = false)
        {
            Remove(obj, key, ignoreCase);
            obj.AddFirst(new JProperty(key, value));
        }

        public static void AddAfter(JObject obj, string afterKey, string key, object value, bool ignoreCase = false)
        {
            var properties = obj.Children<JProperty>().ToList();
            obj.RemoveAll();
            foreach (var property in properties)
            {
                if (!key.Equals(property.Name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    obj.Add(property.Name, property.Value);
                }

                if (afterKey.Equals(property.Name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                {
                    obj.Add(new JProperty(key, value));
                    value = null;
                }
            }

            if (value != null)
            {
                obj.Add(new JProperty(key, value));
            }
        }

        public static void Replace(JToken token, JToken with)
        {
            var prop = token as JProperty;
            if (prop != null && !(with is JProperty))
            {
                token.Replace(new JProperty(prop.Name, with));
            }
            else
            {
                token.Replace(with);
            }
        }

        public static object Remove(JObject obj, string key, bool ignoreCase = false)
        {
            var o = FindToken(obj, key, ignoreCase);
            if (o != null)
            {
                if (ignoreCase && o.Type == JTokenType.Property)
                {
                    key = ((JProperty) o).Name;
                }

                obj.Remove(key);
            }

            return o?.Value<object>();
        }

        public static void RemoveAll(JObject obj, IEnumerable<string> keys, bool ignoreCase = false)
        {
            foreach (var key in keys)
            {
                Remove(obj, key, ignoreCase);
            }
        }

        public static T Clone<T>(JToken token)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(token));
        }

        public static JToken Clone(JToken token)
        {
            return Clone<JToken>(token);
        }

        public static bool IsNullOrEmpty(JToken token)
        {
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.None)
            {
                return true;
            }

            switch (token.Type)
            {
                case JTokenType.String:
                    return string.IsNullOrEmpty(token.Value<string>());
                case JTokenType.Array:
                case JTokenType.Object:
                    return !token.Children().Any();
            }

            return false;
        }

        public static JToken FindToken(JToken parent, string key, bool ignoreCase = false)
        {
            if (parent == null)
            {
                return null;
            }

            if (parent.Type == JTokenType.Property)
            {
                parent = ((JProperty) parent).Value;
            }

            var token = parent.Type == JTokenType.Object ? (parent as JObject).GetTokenByKey(key, ignoreCase) : null;

            if (token != null)
            {
                return token;
            }

            foreach (var child in parent.Children())
            {
                token = FindToken(child, key, ignoreCase);
                if (token != null)
                {
                    break;
                }
            }

            return token;
        }

        public static R GetTokenValue<R>(JToken parent, string key, bool ignoreCase = false)
        {
            if (parent == null)
            {
                return default(R);
            }

            if (parent.Type == JTokenType.Property)
            {
                parent = ((JProperty) parent).Value;
            }

            var token = parent.Type == JTokenType.Object ? (parent as JObject).GetTokenByKey(key, ignoreCase) : null;
            var value = token != null ? token.Value<R>() : default;

            return value;
        }

        public static R FindTokenValue<R>(JToken parent, string key, bool ignoreCase = false)
        {
            if (parent == null)
            {
                return default(R);
            }

            if (parent.Type == JTokenType.Property)
            {
                parent = ((JProperty) parent).Value;
            }

            var value = GetTokenValue<R>(parent, key, ignoreCase);

            if (IsNullOrDefault(value) && parent as JContainer != null)
            {
                foreach (var child in parent.Children())
                {
                    value = FindTokenValue<R>(child, key);
                    if (!IsNullOrDefault(value))
                    {
                        break;
                    }
                }
            }

            if (IsNullOrDefault(value) && key.Contains("."))
            {
                value = GetByPath<R>(parent, key, ignoreCase);
            }

            return value;
        }

        public static JToken GetByPath(JToken parent, string path, bool ignoreCase = false)
        {
            if (parent == null || string.IsNullOrEmpty(path))
            {
                return null;
            }

            return parent.SelectTokenByPath(path, ignoreCase);
        }

        public static void SetByPath(JToken parent, string path, object value, bool ignoreCase = false)
        {
            if (parent == null || string.IsNullOrEmpty(path))
            {
                return;
            }

            var parts = path.Split('.');
            var p = parent;
            var i = 0;
            while (i < parts.Length - 1)
            {
                var o = new JObject();
                AddOrSet(p as JObject, parts[i], o, ignoreCase);
                p = o;
                i += 1;
            }

            AddOrSet(p as JObject, parts[parts.Length - 1], value, ignoreCase);
        }

        public static R GetByPath<R>(JToken parent, string path, bool ignoreCase = false)
        {
            if (parent == null || string.IsNullOrEmpty(path))
            {
                return default;
            }

            var found = parent.SelectTokenByPath(path, ignoreCase);
            return found != null ? found.Value<R>() : default;
        }

        public static JProperty DrillDownToFirstProperty(JToken token)
        {
            if (token == null)
            {
                return null;
            }

            var prop = token as JProperty;
            while (prop == null && token != null && token.Children().Any())
            {
                token = token.Children().First();
                prop = token as JProperty;
            }

            return prop;
        }

        public static JProperty ClimbUpToFirstProperty(JToken token)
        {
            if (token == null)
            {
                return null;
            }

            var prop = token as JProperty;
            while (prop == null && token != null && token.Parent != null)
            {
                token = token.Parent;
                prop = token as JProperty;
            }

            return prop;
        }


        public static R DrillDownToFirstValue<R>(JToken token)
        {
            if (token == null)
            {
                return default;
            }

            var value = token as JValue;
            while (value == null && token != null && token.Children().Any())
            {
                token = token.Children().First();
                value = token as JValue;
            }

            return value != null ? value.Value<R>() : default;
        }

        #region Utilities

        public static JToken GetTokenByKey(JObject obj, string key, bool ignoreCase = false)
        {
            return obj.GetValue(key, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
        }

        public static JToken SelectTokenByPath(JToken token, string path, bool ignoreCase = false)
        {
            var found = token.SelectToken(path);
            if (found == null && ignoreCase)
            {
                var parts = path.Split('.');
                var i = 0;
                var obj = token as JObject;
                while (obj != null && i < parts.Length)
                {
                    token = obj.GetTokenByKey(parts[i], true);
                    obj = token as JObject;
                    i += 1;
                }

                if (i == parts.Length && token != null)
                {
                    found = token;
                }
            }

            return found;
        }

        public static JObject GetPagingJson(Paging p, int total)
        {
            var paging = JObject.FromObject(p);
            paging.Add("total", new JValue(total));
            return paging;
        }

        public static JObject GetJsonFromString(string jsonString)
        {
            var json = JsonConvert.DeserializeObject<JObject>(jsonString);
            return json;
        }

        public static JObject GetJsonFromFile(string filePath)
        {
            JObject json = null;
            if (File.Exists(filePath))
            {
                using (var reader = new StreamReader(filePath))
                {
                    var jsonReader = new JsonTextReader(reader);
                    json = JObject.Load(jsonReader);
                }
            }

            return json;
        }

        public static JObject GetExceptionJson(Exception exception)
        {
            return JsonConvert.DeserializeObject<JObject>(JsonConvert.SerializeObject(exception));
        }

        public static T CastTo<T>(object o) where T : JToken
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(o));
        }

        public static bool IsNullOrDefault<T>(T value)
        {
            return Equals(default(T), value);
        }

        #endregion
    }
}