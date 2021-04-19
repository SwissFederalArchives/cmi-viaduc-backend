using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using CMI.Contract.Parameter.Attributes;
using CMI.Contract.Parameter.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CMI.Contract.Parameter
{
    public class ParameterHelper : IParameterHelper
    {
        private static readonly ConcurrentDictionary<Type, ISetting> settingsCache = new ConcurrentDictionary<Type, ISetting>();
        private static readonly object fileLock = new object();

        /// <summary>
        ///     Ein Setting sollte nicht oder nur sehr kurz zwischengespeichert werden. Sonst sollte das Setting mit dieser Methode
        ///     geholt werden.
        /// </summary>
        public T GetSetting<T>() where T : ISetting
        {
            if (settingsCache.TryGetValue(typeof(T), out var settingFromCache))
            {
                return (T) settingFromCache;
            }

            var setting = GetSettingAsSettingFromFile<T>();

            foreach (var propertyInfo in setting.GetType().GetProperties())
            {
                if ((propertyInfo.GetValue(setting) == null || propertyInfo.GetValue(setting).Equals(0)) && GetDefault(propertyInfo) != null)
                {
                    propertyInfo.SetValue(setting, GetDefault(propertyInfo));
                }
            }

            settingsCache[typeof(T)] = setting;

            return setting;
        }

        public string SaveSetting<T>(Parameter newParameter) where T : ISetting
        {
            if (newParameter == null)
            {
                throw new ArgumentNullException(nameof(newParameter));
            }

            if (!Validate(newParameter))
            {
                return "Invalider Parameter";
            }

            var jObj = GetSettingAsJObject<T>();

            var parameterPropertyNameWithoutPrefix = newParameter.Name.Split('.').Last();
            var pi = typeof(T).GetProperties().First(p => p.Name == parameterPropertyNameWithoutPrefix);
            var defaultWert = GetDefault(pi);

            if (defaultWert != null && defaultWert.Equals(newParameter.Value))
            {
                jObj.Remove(parameterPropertyNameWithoutPrefix);
            }
            else
            {
                jObj[parameterPropertyNameWithoutPrefix] = new JValue(newParameter.Value);
            }

            try
            {
                lock (fileLock)
                {
                    CreateDirectory(typeof(T));
                    var path = GetSettingPath(typeof(T));
                    File.WriteAllText(path, JsonConvert.SerializeObject(jObj), Encoding.UTF8);
                }

                OnParameterSaved(new ParameterSavedEventArgs {Parameter = newParameter, SettingType = typeof(T)});
                settingsCache.TryRemove(typeof(T), out _);

                return string.Empty;
            }
            catch
            {
                return "Error während dem Speichern";
            }
        }

        public static event EventHandler<ParameterSavedEventArgs> ParameterSaved;

        private JObject GetSettingAsJObject<T>() where T : ISetting
        {
            lock (fileLock)
            {
                var path = GetSettingPath(typeof(T));
                return File.Exists(path)
                    ? JObject.Parse(File.ReadAllText(path))
                    : new JObject();
            }
        }

        public static void OnParameterSaved(ParameterSavedEventArgs e)
        {
            var handler = ParameterSaved;
            handler?.Invoke(null, e);
        }

        public Parameter[] GetSettingAsParamListFromFile<T>() where T : ISetting
        {
            var setting = GetSettingAsJObject<T>();

            var paramList = new List<Parameter>();
            foreach (var propertyInfo in typeof(T).GetProperties())
            {
                paramList.Add(Create(typeof(T), propertyInfo, (JValue) setting.GetValue(propertyInfo.Name)));
            }

            return paramList.ToArray();
        }

        private T GetSettingAsSettingFromFile<T>() where T : ISetting
        {
            var parameters = GetSettingAsParamListFromFile<T>();
            var obj = (T) Activator.CreateInstance(typeof(T));
            var properties = typeof(T).GetProperties();

            foreach (var p in parameters)
            {
                var parameterPropertyNameWithoutPrefix = p.Name.Split('.').Last();
                object valueInCorrectTargetType;

                var propertyInfo = properties.First(property => property.Name == parameterPropertyNameWithoutPrefix);

                var propertyTargetType = propertyInfo.PropertyType;

                if (p.Value == null)
                {
                    valueInCorrectTargetType = null;
                }
                else
                {
                    if (p.Value.GetType() == propertyTargetType)
                    {
                        valueInCorrectTargetType = p.Value;
                    }
                    else
                    {
                        // Für den Fall, dass im JSON FIle des Typ der Eigenschaft nicht genau übereinstimmt
                        var converter = TypeDescriptor.GetConverter(propertyTargetType);
                        if (converter != null)
                        {
                            valueInCorrectTargetType = converter.ConvertFrom(p.Value);
                        }
                        else
                        {
                            valueInCorrectTargetType = p.Value;
                        }
                    }
                }

                propertyInfo.SetValue(obj, valueInCorrectTargetType);
            }

            return obj;
        }


        public static object GetDefault(PropertyInfo propertyInfo)
        {
            var attr = (DefaultValueAttribute) Attribute.GetCustomAttribute(propertyInfo,
                typeof(DefaultValueAttribute), true);

            if (attr != null)
            {
                return attr?.Value;
            }

            var readFromResource =
                (ReadDefaultFromResourceAttribute) Attribute.GetCustomAttribute(propertyInfo, typeof(ReadDefaultFromResourceAttribute), true);
            if (readFromResource == null)
            {
                return null;
            }

            var valueFromResource = ReadDefaultFromResource(propertyInfo.DeclaringType, propertyInfo);
            return valueFromResource;
        }

        private static string ReadDefaultFromResource(Type type, PropertyInfo propInfo)
        {
            var assembly = type.Assembly;
            var resourceName = assembly.GetName().Name + ".Defaults." + type.Name + "." + propInfo.Name;

            // mit verschiedenen Extensions probieren
            // Editieren der Templates in VS ist einfacher wenn .mustache extension verwendet wird!
            return ReadFromResource(assembly, resourceName)
                   ?? ReadFromResource(assembly, $"{resourceName}.mustache")
                   ?? ReadFromResource(assembly, $"{resourceName}.txt");
        }

        private static string ReadFromResource(Assembly assembly, string resourceName)
        {
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    return null;
                }

                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
        [SuppressMessage("ReSharper", "HeuristicUnreachableCode")]
        private string GetSettingPath(Type type)
        {
            var settingName = type.Name + ".json";

            if (HttpRuntime.AppDomainAppId != null)
            {
                if (string.IsNullOrEmpty(ParameterSettings.Default.Path))
                {
                    return Path.Combine(AppDomain.CurrentDomain.GetData("DataDirectory").ToString(), "Parameters", settingName);
                }

                var dirInfo = new DirectoryInfo(HttpRuntime.AppDomainAppPath);
                var iisDirectoryName = dirInfo.Name;
                var replaced = ParameterSettings.Default.Path.Replace("{exeName}", iisDirectoryName);
                return Path.Combine(replaced, settingName);
            }

            if (string.IsNullOrEmpty(ParameterSettings.Default.Path))
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Parameters", settingName);
            }

            {
                var exeName = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName); // z.B. CMI.Host.Cache
                var replaced = ParameterSettings.Default.Path.Replace("{exeName}", exeName);
                return Path.Combine(replaced, settingName);
            }
        }

        private void CreateDirectory(Type type)
        {
            var path = GetSettingPath(type);
            var dir = Path.GetDirectoryName(path);
            var info = new DirectoryInfo(dir);
            if (!info.Exists)
            {
                info.Create();
            }
        }

        private static string GetHtmlEditorType(Type type)
        {
            if (type.Name == "Boolean")
            {
                return "checkbox";
            }

            if (type.Name == "Int32" || type.Name == "Double" || type.Name == "Float" || type.Name == "Int64" || type.Name == "Long")
            {
                return "number";
            }

            return "textarea";
        }

        public static Parameter Create(Type settingType, PropertyInfo propertyInfo, JValue jValue)
        {
            var propertyname = propertyInfo.Name;

            Debug.WriteLine(propertyname);

            var parameter = new Parameter
            {
                Name = settingType.Namespace + "." + settingType.Name + "." + propertyInfo.Name,
                Value = jValue == null
                    ? GetDefault(propertyInfo)
                    : jValue.Value,
                Type = GetHtmlEditorType(propertyInfo.PropertyType),
                Mandatory = ((MandatoryAttribute) Attribute.GetCustomAttribute(propertyInfo, typeof(MandatoryAttribute), true))?.IsMandatory == true,
                Default = GetDefault(propertyInfo),
                Description = ((DescriptionAttribute) Attribute.GetCustomAttribute(propertyInfo, typeof(DescriptionAttribute), true))?.Description,
                RegexValidation = ((ValidationAttribute) Attribute.GetCustomAttribute(propertyInfo, typeof(ValidationAttribute), true))?.Regex,
                ErrrorMessage = ((ValidationAttribute) Attribute.GetCustomAttribute(propertyInfo, typeof(ValidationAttribute), true))?.Message
            };

            return parameter;
        }

        public static bool Validate(Parameter parameter)
        {
            if (parameter.Value != null && string.IsNullOrEmpty(parameter.Value.ToString()) && parameter.Mandatory)
            {
                return false;
            }

            if (parameter.RegexValidation == null || parameter.Value == null)
            {
                return true;
            }

            var regex = new Regex(parameter.RegexValidation);

            var match = regex.IsMatch(parameter.Value.ToString());
            return match;
        }
    }

    public class ParameterSavedEventArgs : EventArgs
    {
        public Type SettingType { get; set; }
        public Parameter Parameter { get; set; }
    }
}