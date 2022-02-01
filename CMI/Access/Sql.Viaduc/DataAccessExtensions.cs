using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Newtonsoft.Json.Linq;

namespace CMI.Access.Sql.Viaduc
{
    public static class DataAccessExtensions
    {
        public static object ToDbParameterValue(this object obj)
        {
            if (obj == null)
            {
                return DBNull.Value;
            }

            if (obj is string s && string.IsNullOrWhiteSpace(s))
            {
                return DBNull.Value;
            }

            return obj;
        }

        public static void AddParameter(this SqlCommand cmd, string name, SqlDbType dbType, object value)
        {
            cmd.Parameters.Add(new SqlParameter
            {
                ParameterName = name,
                SqlDbType = dbType,
                Value = value.ToDbParameterValue()
            });
        }

        public static Dictionary<string, PropertyInfo> GetPropertiesOfType<T>(this Type onType)
        {
            var infos = new Dictionary<string, PropertyInfo>();
            var bind = BindingFlags.Public | BindingFlags.Instance;
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
                        infos.Add(p.Name, p);
                    }
                }
            }

            return infos;
        }

        private static T? ToEnum<T>(object obj) where T : struct, IConvertible
        {
            if (obj == DBNull.Value)
            {
                return null;
            }

            if (!typeof(T).IsEnum)
            {
                throw new ArgumentException("T must be an enumerated type");
            }

            var intValue = Convert.ToInt32(obj);

            if (!Enum.IsDefined(typeof(T), intValue))
            {
                throw new ArgumentException("The Int32 value " + intValue + " can not be converted into an enum of type " + typeof(T).FullName);
            }

            return (T) Enum.ToObject(typeof(T), intValue);
        }

        public static void PopulateProperties<T>(this SqlDataReader reader, T obj) where T : class
        {
            var dataReaderFieldNames = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)).ToArray();
            MapAllFields(obj, s => Convert.ToInt32(reader[s]), dataReaderFieldNames);
            MapAllFields(obj, s => reader[s] as string,
                dataReaderFieldNames); // https://github.com/CMInformatik/Viaduc/wiki/Null-und-string.Empty-Handling
            MapAllFields(obj, s => Convert.ToDateTime(reader[s]), dataReaderFieldNames);
            MapAllFields(obj, s => DateTime.TryParse(Convert.ToString(reader[s]), out var dateValue) ? dateValue : (DateTime?) null,
                dataReaderFieldNames);
            MapAllFields(obj, s => Convert.ToBoolean(reader[s]), dataReaderFieldNames);
            MapAllFields(obj, s => reader[s], dataReaderFieldNames);

            MapAllFields(obj, s =>
            {
                var result = new List<string>();
                var dbValue = Convert.ToString(reader[s]);

                if (!string.IsNullOrEmpty(dbValue))
                {
                    result = dbValue.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                return result;
            }, dataReaderFieldNames);

            MapAllFields(obj, s =>
            {
                var json = Convert.ToString(reader[s]);
                return !string.IsNullOrEmpty(json) ? JObject.Parse(json) : new JObject();
            }, dataReaderFieldNames);

            MapXmlFields(obj, reader);
        }

        public static T? GetValueOrNull<T>(this object value) where T : struct
        {
            string fullNameSoll = typeof(T).FullName;
            string fullName = value != null ? value.GetType().FullName : string.Empty;

            if (string.IsNullOrEmpty(fullNameSoll) || string.IsNullOrEmpty(fullName))
            {
                return null;
            }

            if (fullName.Equals(fullNameSoll))
            {
                return value is DBNull ? null : (T?)value;
            }
            if (fullNameSoll.Equals(typeof(int).FullName) )
            {
                if (int.TryParse(value?.ToString(), out var result))
                {
                    return result as T?;
                }
            }
            if (fullNameSoll.Equals(typeof(short).FullName))
            {
                if (short.TryParse(value?.ToString(), out var result))
                {
                    return result as T?;
                }
            }
            if (fullNameSoll.Equals(typeof(long).FullName))
            {
                if (long.TryParse(value?.ToString(), out var result))
                {
                    return result as T?;
                }
            }
            if (fullNameSoll.Equals(typeof(double).FullName))
            {
                if (double.TryParse(value?.ToString(), out var result))
                {
                    return result as T?;
                }
            }

            return null;
        }

        private static void MapAllFields<TTargetType, TTargetFieldType>(TTargetType targetObj, Func<string, TTargetFieldType> convert,
            string[] dataReaderFieldNames)
        {
            var targetPropertiesByName = typeof(TTargetType).GetPropertiesOfType<TTargetFieldType>();
            foreach (var property in targetPropertiesByName)
            {
                if (dataReaderFieldNames.Contains(property.Key))
                {
                    property.Value.SetValue(targetObj, convert(property.Key), null);
                }
            }
        }

        private static void MapAllFields<TTargetType>(TTargetType targetObj, Func<string, object> convert, string[] dataReaderFieldNames)
        {
            var targetPropertiesByName = typeof(TTargetType).GetPropertiesOfType<Enum>();
            foreach (var property in targetPropertiesByName)
            {
                if (dataReaderFieldNames.Contains(property.Key))
                {
                    property.Value.SetValue(targetObj, convert(property.Key), null);
                }
            }
        }

        private static void MapXmlFields<TTargetType>(TTargetType targetObj, SqlDataReader reader)
        {
            for (var i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetDataTypeName(i) != "xml")
                {
                    continue;
                }

                if (reader.IsDBNull(i))
                {
                    continue;
                }

                var xml = reader.GetString(i);
                var fieldName = reader.GetName(i);

                var targetPropertyInfo = typeof(TTargetType).GetProperty(fieldName);
                if (targetPropertyInfo == null)
                {
                    continue;
                }

                var s = new XmlSerializer(targetPropertyInfo.PropertyType);
                var obj = s.Deserialize(new StringReader(xml));
                targetPropertyInfo.SetValue(targetObj, obj);
            }
        }

        public static object CreateNewItem(string connectionString, string query, List<SqlParameter> parameterList)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return null;
            }

            if (string.IsNullOrEmpty(query))
            {
                return null;
            }

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = query;

                    foreach (var sqlParameter in parameterList)
                    {
                        cmd.Parameters.Add(sqlParameter);
                    }

                    return cmd.ExecuteScalar();
                }
            }
        }

        public static void ExecuteQuery(string connectionString, string query, List<SqlParameter> parameterList = null)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return;
            }

            if (string.IsNullOrEmpty(query))
            {
                return;
            }

            using (var cn = new SqlConnection(connectionString))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    cmd.CommandText = query;

                    if (parameterList != null)
                    {
                        foreach (var sqlParameter in parameterList)
                        {
                            cmd.Parameters.Add(sqlParameter);
                        }
                    }

                    cmd.ExecuteScalar();
                }
            }
        }

        public static void AddModifiedDataToCommand(this SqlCommand cmd, string userId, string modifiedByUserId)
        {
            cmd.CommandText +=
                " UPDATE ApplicationUser SET modifiedOn = @modifiedOn, modifiedBy = (SELECT emailAddress FROM ApplicationUser WHERE ID = @modifiedByUserId) WHERE ID = @modifiedUserId ";

            cmd.AddParameter("modifiedOn", SqlDbType.DateTime, DateTime.Now);
            cmd.AddParameter("modifiedByUserId", SqlDbType.NVarChar, modifiedByUserId);
            cmd.AddParameter("modifiedUserId", SqlDbType.NVarChar, userId);
        }
    }
}