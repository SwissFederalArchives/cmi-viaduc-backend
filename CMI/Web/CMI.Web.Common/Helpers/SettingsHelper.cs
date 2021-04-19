using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CMI.Utilities.Common.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace CMI.Web.Common.Helpers
{
    public class SettingsEntry
    {
    }

    public static class SettingsHelper
    {
        public const string InternalAttribute = "_internal";
        public const string PrivateAttribute = "_private";
        public const string ReplaceAttribute = "_replace";
        public const string RemoveAttribute = "_remove";
        public const string ExtendAttribute = "_extend";

        public const string FileAttribute = "_file";

        public static readonly HashSet<string> ControlAttributesToCleanup;

        static SettingsHelper()
        {
            ControlAttributesToCleanup = new HashSet<string>
            {
                InternalAttribute,
                PrivateAttribute,
                ReplaceAttribute,
                RemoveAttribute,
                ExtendAttribute
            };
        }

        public static T GetSettingsFor<T>(JObject settings, string entryPath) where T : SettingsEntry
        {
            T entry = default;
            try
            {
                if (settings != null)
                {
                    var jsonEntry = JsonHelper.GetByPath(settings, entryPath);
                    if (jsonEntry != null)
                    {
                        entry = JsonConvert.DeserializeObject<T>(jsonEntry.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error on GetSettingsFor settings:{@settings} entryPath:{entryPath}", settings, entryPath);
            }

            return entry;
        }

        private static void AddOrSetSettings(JContainer left, JProperty right, bool allowInternal, bool extendByDefault = false)
        {
            if (right.Value.Type == JTokenType.Object)
            {
                var obj = new JObject();
                JsonHelper.AddOrSet(left as JObject, right.Name, obj);
                UpdateSettingsWith(obj, right.Value, allowInternal, extendByDefault);
            }
            else if (right.Value.Type == JTokenType.Array)
            {
                var arr = new JArray();
                JsonHelper.AddOrSet(left as JObject, right.Name, arr);
                UpdateSettingsWith(arr, right.Value, allowInternal, extendByDefault);
            }
            else if (left is JObject)
            {
                JsonHelper.AddOrSet(left as JObject, right.Name, right.Value.DeepClone());
            }
            else if (left is JArray)
            {
                UpdateSettingsWith(left as JArray, right.Value as JArray, allowInternal, extendByDefault);
            }
        }

        public static void UpdateSettingsWith(JContainer left, JProperty right, bool allowInternal, bool extendByDefault = false)
        {
            if (!allowInternal && IsInternalOnly(right))
            {
                return;
            }

            JToken leftProperty = null;
            try
            {
                leftProperty = left.SelectToken(right.Name);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to select token by {Name}", right.Name);
            }

            if (leftProperty == null)
            {
                if (!DoRemove(right))
                {
                    AddOrSetSettings(left, right, allowInternal, extendByDefault);
                }
            }
            else
            {
                var leftObject = leftProperty as JObject;
                if (leftObject != null && DoRemove(right))
                {
                    var leftParent = leftObject.Parent;
                    leftParent.Remove();
                }
                else if (leftObject == null || !DoExtendBy(right, extendByDefault))
                {
                    var leftParent = (JProperty) leftProperty.Parent;
                    var leftContainer = leftParent.Value as JContainer;
                    if (leftContainer != null)
                    {
                        AddOrSetSettings(leftContainer, right, allowInternal, extendByDefault);
                    }
                    else
                    {
                        leftParent.Value = right.Value.DeepClone();
                    }
                }
                else
                {
                    var extend = FindAttributeValue<bool?>(right, ExtendAttribute, null);
                    UpdateSettingsWith(leftObject, right.Value, allowInternal, extend.GetValueOrDefault(extendByDefault));
                }
            }
        }

        public static void UpdateSettingsWith(JContainer left, JToken right, bool allowInternal, bool extendByDefault = false)
        {
            if (left != null && right != null)
            {
                if (right is JObject)
                {
                    foreach (var rightChild in right.Children<JProperty>())
                    {
                        UpdateSettingsWith(left, rightChild, allowInternal, extendByDefault);
                    }
                }
                else if (right is JArray)
                {
                    var rightArray = right as JArray;
                    foreach (var rightChild in rightArray.Children<JToken>())
                    {
                        if (rightChild is JObject)
                        {
                            var obj = new JObject();
                            UpdateSettingsWith(obj, rightChild, allowInternal, extendByDefault);
                            left.Add(obj);
                        }
                        else
                        {
                            left.Add(rightChild.DeepClone());
                        }
                    }
                }
                else if (right is JProperty)
                {
                    AddOrSetSettings(left, right as JProperty, allowInternal, extendByDefault);
                }
            }
        }

        public static void CleanupSettings(JToken settings, HashSet<string> attributesToCleanup)
        {
            if (settings != null)
            {
                settings.RemoveDescendantsByName(attributesToCleanup);
            }
        }

        public static void CleanupSettings(JToken settings)
        {
            CleanupSettings(settings, ControlAttributesToCleanup);
        }

        public static void RemovePrivateSettings(JToken settings)
        {
            var container = settings as JContainer;
            if (container == null)
            {
                // nuttin
            }
            else if (IsPrivateOnly(container))
            {
                container.Remove();
            }
            else
            {
                var subContainers = container.Children<JContainer>().ToList();
                foreach (var subContainer in subContainers)
                {
                    RemovePrivateSettings(subContainer);
                }
            }
        }

        public static void ProcessFiles(string basePath, JToken settings)
        {
            var container = settings as JContainer;
            if (container == null)
            {
                return;
            }

            var filePath = FindAttributeValue<string>(container, FileAttribute);
            if (!string.IsNullOrEmpty(filePath))
            {
                var replace = string.Empty;
                filePath = StringHelper.AddToString(basePath, @"\", filePath.Replace("/", @"\"));
                if (File.Exists(filePath))
                {
                    replace = File.ReadAllText(filePath).ToBase64String();
                }

                JsonHelper.Replace(container, new JValue(replace));
            }
            else
            {
                var subContainers = container.Children<JContainer>().ToList();
                foreach (var subContainer in subContainers)
                {
                    ProcessFiles(basePath, subContainer);
                }
            }
        }

        #region Utilities

        public static R FindAttributeValue<R>(JToken token, string key, R defaultValue = default)
        {
            JToken attribute = null;
            if (token.Type == JTokenType.Property)
            {
                attribute = JsonHelper.GetByPath((token as JProperty).Value, key);
            }
            else
            {
                attribute = JsonHelper.GetByPath(token, key);
            }

            return attribute != null ? attribute.Value<R>() : defaultValue;
        }

        public static JObject InjectInfo(JObject settings, string path, string key, object value)
        {
            var container = JsonHelper.GetByPath<JObject>(settings, path);
            if (container == null)
            {
                container = new JObject();
                JsonHelper.SetByPath(settings, path, container);
            }

            JsonHelper.AddOrSet(container, key, value);

            return container;
        }

        public static JObject InjectServiceAssemblyInfo(JObject settings, Assembly assembly)
        {
            try
            {
                var version = assembly.GetName().Version;
                return InjectInfo(settings, "service", "version",
                    string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision));
            }
            catch
            {
                // service info is not critical   
            }

            return null;
        }

        #endregion

        #region Private

        private static bool IsInternalOnly(JToken token)
        {
            return FindAttributeValue(token, InternalAttribute, false);
        }

        private static bool IsPrivateOnly(JToken token)
        {
            return FindAttributeValue(token, PrivateAttribute, false);
        }

        private static bool DoReplace(JToken token)
        {
            return FindAttributeValue(token, ReplaceAttribute, false);
        }

        private static bool DoRemove(JToken token)
        {
            return FindAttributeValue(token, RemoveAttribute, false);
        }

        private static bool DoExtendBy(JToken token, bool extendByDefault)
        {
            var extend = FindAttributeValue(token, ExtendAttribute, extendByDefault);
            if (extend && (DoReplace(token) || DoRemove(token)))
            {
                extend = false;
            }

            return extend;
        }

        #endregion
    }
}