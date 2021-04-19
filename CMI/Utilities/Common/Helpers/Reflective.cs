using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CMI.Utilities.Common.Helpers
{
    public static class Reflective
    {
        public static bool IsNullOrDefault<T>(T value)
        {
            return Equals(default(T), value);
        }

        /// <summary>
        ///     Get property (or field) value for type (or instance if not null) if present. Else return default of T.
        /// </summary>
        public static T GetValue<T>(Type onType, string name, object onInstance = null,
            BindingFlags access = BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (onInstance != null && onInstance is IDictionary)
            {
                var dictionary = onInstance as IDictionary;
                if (dictionary.Contains(name))
                {
                    return (T) dictionary[name];
                }
            }
            else
            {
                var bind = GetDefaultBindingFlags(access, onInstance);
                var p = onType.GetProperty(name, bind);
                if (p != null)
                {
                    // we dont support indexers, so GetMethod.Parameters must be empty
                    var m = p.GetGetMethod();
                    if (m != null && m.GetParameters().Length <= 0)
                    {
                        return (T) p.GetValue(onInstance, null);
                    }
                }

                var f = onType.GetField(name, bind);
                if (f != null)
                {
                    return (T) f.GetValue(onInstance);
                }
            }

            return default;
        }

        /// <summary>
        ///     Set property (or field) for type (or instance if not null) if present. Return whether the value was set.
        /// </summary>
        public static bool SetValue<T>(Type onType, string name, T value, object onInstance = null,
            BindingFlags access = BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (onInstance != null && onInstance is IDictionary)
            {
                var dictionary = onInstance as IDictionary;
                dictionary[name] = value;
            }
            else
            {
                var bind = GetDefaultBindingFlags(access, onInstance);
                var p = onType.GetProperty(name, bind);
                if (p != null)
                {
                    // we dont support indexers, so GetMethod.Parameters must be empty
                    var m = p.GetGetMethod();
                    if (m != null && m.GetParameters().Length <= 0)
                    {
                        p.SetValue(onInstance, value, null);
                        return true;
                    }
                }

                var f = onType.GetField(name, bind);
                if (f != null)
                {
                    f.SetValue(onInstance, value);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Get property (or field) value for (sub-)attribute of an object if present. Else return default of T.
        /// </summary>
        public static T GetValue<T>(object onObject, string path)
        {
            object onInstance;
            string name;
            if (DrilldownToInstance(onObject, path, out onInstance, out name))
            {
                return GetValue<T>(onInstance.GetType(), name, onInstance);
            }

            return default;
        }

        /// <summary>
        ///     Set property (or field) for instance if present. Return whether the value was set.
        /// </summary>
        public static bool SetValue<T>(object onObject, string path, T value)
        {
            object onInstance;
            string name;
            if (DrilldownToInstance(onObject, path, out onInstance, out name))
            {
                return SetValue(onInstance.GetType(), name, value, onInstance);
            }

            return false;
        }

        /// <summary>
        ///     Set value (of type T) of thisObject from another fromObject conditionally:
        ///     either forced by useThisValue = false, or in case thisObject.GetValue equals default of T
        /// </summary>
        public static bool SetValueFrom<T>(object thisObject, object fromObject, string thisName, string fromName, bool useThisValue,
            T fromValue = default)
        {
            if (!useThisValue || IsNullOrDefault(GetValue<T>(thisObject, thisName)))
            {
                if (IsNullOrDefault(fromValue) && !string.IsNullOrEmpty(fromName))
                {
                    fromValue = GetValue<T>(fromObject, fromName);
                }

                return SetValue(thisObject, thisName, fromValue);
            }

            return false;
        }

        public static bool SetValueFrom<T>(object thisObject, object fromObject, string thisName, bool useThisValue, T fromValue = default)
        {
            return SetValueFrom(thisObject, fromObject, thisName, null, useThisValue, fromValue);
        }

        public static bool SetValueFrom<T>(object thisObject, object fromObject, string name, T fromValue = default)
        {
            return SetValueFrom<T>(thisObject, fromObject, name, name, false);
        }

        /// <summary>
        ///     Get infos on properties of a certain type
        /// </summary>
        public static Dictionary<string, T> GetProperties<T>(Type onType, object onInstance = null,
            BindingFlags access = BindingFlags.Public | BindingFlags.NonPublic)
        {
            var bind = GetDefaultBindingFlags(access, onInstance);
            var infos = new Dictionary<string, T>();
            var ps = onType.GetProperties(bind);
            var t = typeof(T);
            for (var i = 0; i < ps.Length; i++)
            {
                var p = ps[i];
                if (t.IsAssignableFrom(p.PropertyType))
                {
                    // we dont support indexers, so GetMethod.Parameters must be empty
                    var m = p.GetGetMethod();
                    if (m != null && m.GetParameters().Length <= 0)
                    {
                        var value = onInstance == null && (bind & BindingFlags.Instance) == BindingFlags.Instance
                            ? default
                            : (T) p.GetValue(onInstance, null);
                        infos.Add(p.Name, value);
                    }
                }
            }

            return infos;
        }

        /// <summary>
        ///     Get infos on fields of a certain type
        /// </summary>
        public static Dictionary<string, T> GetFields<T>(Type onType, object onInstance = null,
            BindingFlags access = BindingFlags.Public | BindingFlags.NonPublic)
        {
            var bind = GetDefaultBindingFlags(access, onInstance);
            var infos = new Dictionary<string, T>();
            var fs = onType.GetFields(bind);
            var t = typeof(T);
            for (var i = 0; i < fs.Length; i++)
            {
                var f = fs[i];
                if (t.IsAssignableFrom(f.FieldType))
                {
                    var value = onInstance == null && (bind & BindingFlags.Instance) == BindingFlags.Instance ? default : (T) f.GetValue(onInstance);
                    infos.Add(f.Name, value);
                }
            }

            return infos;
        }

        /// <summary>
        ///     Get infos on fields of a certain type
        /// </summary>
        public static Dictionary<string, T> GetConstants<T>(Type onType, BindingFlags access = BindingFlags.Public | BindingFlags.Static)
        {
            var bind = access | BindingFlags.FlattenHierarchy;
            var t = typeof(T);
            return onType.GetFields(bind).Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == t)
                .ToDictionary(f => f.Name, f => (T) f.GetRawConstantValue());
        }

        /// <summary>
        ///     HasProperty
        /// </summary>
        public static bool HasProperty(Type onType, string name, out object value, object onInstance = null,
            BindingFlags access = BindingFlags.Public | BindingFlags.NonPublic)
        {
            var bind = GetDefaultBindingFlags(access, onInstance);
            value = null;
            var p = onType.GetProperty(name, bind);
            if (p != null)
            {
                value = p.GetValue(onInstance, null);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     HasField
        /// </summary>
        public static bool HasField(Type onType, string name, out object value, object onInstance = null,
            BindingFlags access = BindingFlags.Public | BindingFlags.NonPublic)
        {
            var bind = GetDefaultBindingFlags(access, onInstance);
            value = null;
            var f = onType.GetField(name, bind);
            if (f != null)
            {
                value = f.GetValue(onInstance);
                return true;
            }

            return false;
        }

        /// <summary>
        ///     HasProperyOrField
        /// </summary>
        public static bool HasProperyOrField(Type onType, string name, out object value, object onInstance = null,
            BindingFlags access = BindingFlags.Public | BindingFlags.NonPublic)
        {
            if (HasProperty(onType, name, out value, onInstance, access))
            {
                return true;
            }

            return HasField(onType, name, out value, onInstance, access);
        }

        /// <summary>
        ///     DrilldownToInstance
        /// </summary>
        public static bool DrilldownToInstance(object onObject, string path, out object instance, out string name, bool ignoreCase = false,
            BindingFlags access = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
        {
            instance = onObject;
            name = path;
            if (!string.IsNullOrEmpty(path) && path.Contains("."))
            {
                var parts = path.Split('.');
                var i = 0;
                while (i < parts.Length - 1 && instance != null)
                {
                    name = parts[i];
                    if (instance is IDictionary)
                    {
                        var dictionary = instance as IDictionary;
                        instance = null;
                        if (ignoreCase)
                        {
                            var n = name.ToUpperInvariant();
                            name = (dictionary.Keys as IEnumerable<string>).FirstOrDefault(k => k.ToUpperInvariant().Equals(n));
                        }

                        if (!string.IsNullOrEmpty(name) && dictionary.Contains(name))
                        {
                            instance = dictionary[name];
                        }
                    }
                    else
                    {
                        if (ignoreCase)
                        {
                            name = LookupCaseSensitiveName(instance, name.ToUpperInvariant(), access);
                        }

                        instance = !string.IsNullOrEmpty(name) ? GetValue<object>(instance.GetType(), name, instance) : null;
                    }

                    i++;
                }

                if (instance != null)
                {
                    name = parts[i];
                    if (ignoreCase)
                    {
                        name = LookupCaseSensitiveName(instance, name, access);
                    }
                }
            }

            return instance != null;
        }

        /// <summary>
        ///     Get all types matching name space
        /// </summary>
        public static List<Type> GetTypesInNamespace(Assembly assembly, string nameSpace)
        {
            return assembly.GetTypes().Where(t => t.Namespace.StartsWith(nameSpace, StringComparison.Ordinal)).ToList();
        }

        #region Assemblies

        public static Type LoadTypeFromAssembly(string assemblyName, string className)
        {
            var assembly = AppDomain.CurrentDomain.Load(assemblyName);
            return assembly.GetType(className);
        }

        #endregion

        #region Constants

        public const BindingFlags BindingInstanceAll = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public const BindingFlags BindingInstancePublic = BindingFlags.Instance | BindingFlags.Public;
        public const BindingFlags BindingInstanceNonPublic = BindingFlags.Instance | BindingFlags.NonPublic;

        #endregion

        #region Private Methods

        private static BindingFlags GetDefaultBindingFlags(BindingFlags access, object onInstance = null)
        {
            var bind = access;
            if ((bind & BindingFlags.Static) != BindingFlags.Static && (bind & BindingFlags.Instance) != BindingFlags.Instance)
            {
                bind = bind | (onInstance == null ? BindingFlags.Static : BindingFlags.Instance);
            }

            return bind;
        }

        private static string LookupCaseSensitiveName(object instance, string name, BindingFlags access)
        {
            var bind = GetDefaultBindingFlags(access, instance);
            var t = instance.GetType();
            var n = name.ToUpperInvariant();
            var p = t.GetProperties(bind).FirstOrDefault(o => o.Name.ToUpperInvariant().Equals(n));
            if (p != null)
            {
                name = p.Name;
            }
            else
            {
                var f = t.GetFields(bind).FirstOrDefault(o => o.Name.ToUpperInvariant().Equals(n));
                if (f != null)
                {
                    name = f.Name;
                }
            }

            return name;
        }

        #endregion

        #region Methods

        public static MethodInfo GetMethod(Type onType, string methodName,
            BindingFlags access = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type[] types = null)
        {
            return types != null ? onType.GetMethod(methodName, access, null, types, null) : onType.GetMethod(methodName, access);
        }

        public static object InvokeMethod(MethodInfo method, object onInstance)
        {
            return method.Invoke(onInstance, null);
        }

        public static object InvokeMethod(MethodInfo method, object onInstance, object[] parameters)
        {
            var parInfos = method.GetParameters();
            if (parInfos.Length > parameters.Length)
            {
                Array.Resize(ref parameters, parInfos.Length);
            }

            return method.Invoke(onInstance, parameters);
        }

        #endregion

        #region Types / Interfaces

        public static bool ImplementsType(this Type type, Type baseType)
        {
            return baseType.IsAssignableFrom(type);
        }

        public static bool ImplementsType<T>(this Type type) where T : class
        {
            return typeof(T).IsAssignableFrom(type);
        }

        public static bool ImplementsInterface<I>(this Type type) where I : class
        {
            var interfaceType = typeof(I);

            if (!interfaceType.IsInterface)
            {
                throw new InvalidOperationException("Only interfaces can be implemented.");
            }

            return interfaceType.IsAssignableFrom(type);
        }

        #endregion
    }
}