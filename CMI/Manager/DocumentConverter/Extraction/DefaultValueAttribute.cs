using System;
using System.Linq;
using System.Runtime.CompilerServices;

namespace CMI.Manager.DocumentConverter.Extraction
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultValueAttribute : Attribute
    {
        public DefaultValueAttribute(string value)
        {
            Value = value;
        }

        public DefaultValueAttribute(int value) : this(value.ToString())
        {
        }

        public string Value { get; set; }


        public static string DefaultValue<T>(string defaultValue, [CallerMemberName] string propertyName = "")
        {
            var props = typeof(T).GetProperties();

            var prop = props.FirstOrDefault(p => p.Name == propertyName);

            if (prop == null)
            {
                return defaultValue;
            }

            var attr = prop.GetCustomAttributes(typeof(DefaultValueAttribute), true).SingleOrDefault() as DefaultValueAttribute;

            if (attr != null)
            {
                return attr.Value;
            }

            return defaultValue;
        }
    }
}