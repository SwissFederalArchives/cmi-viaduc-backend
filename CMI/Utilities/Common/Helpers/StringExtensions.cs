using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace CMI.Utilities.Common.Helpers
{
    public static class StringExtensions
    {
        private static readonly Regex ReplacementMatcher = new Regex(@"(?<start>\{)+(?<property>[\w\.\[\]]+)(?<format>:[^}]+)?(?<end>\})+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static string FormatWith(this string format, object source)
        {
            return FormatWith(format, null, source);
        }

        /// <summary>
        ///     Diese Methode MUSS auch aus Security Gründen aufgerufen werden! Weitere Informationen dazu siehe folgender
        ///     Kommentar.
        /// </summary>
        public static string Escape(this string value, string fieldName = null)
        {
            // !!! Achtung !!!
            // Der Doppelpunkt MUSS escaped werden aus Security Gründen (Damit nicht in einem Feld gesucht werden kann, auf das der Benutzer nicht berechtigt ist)
            var escapeArrayCommon = new[] {"\\", "/", ":", "!", "{", "}", "[", "]", "&&", "||"};
            var escapeArraySpecialFields = new[] {"\\", "/", ":", "!", "(", ")", "{", "}", "[", "]", "=", "^", "~", "&&", "||"};

            string[] escapeArray;
            var retValue = value;

            if (fieldName == "formerReferenceCode" || fieldName == "customFields.aktenzeichen" || fieldName == "customFields.früheresAktenzeichen")
            {
                escapeArray = escapeArraySpecialFields;
            }
            else
            {
                escapeArray = escapeArrayCommon;
            }

            foreach (var stringToEscape in escapeArray)
            {
                retValue = retValue.Replace(stringToEscape, "\\" + stringToEscape);
            }

            return retValue;
        }

        public static string FormatWith(this string format, IFormatProvider provider, object source)
        {
            if (format == null)
            {
                throw new ArgumentNullException("format");
            }

            var values = new List<object>();
            var rewrittenFormat = ReplacementMatcher.Replace(format, delegate(Match m)
            {
                var startGroup = m.Groups["start"];
                var propertyGroup = m.Groups["property"];
                var formatGroup = m.Groups["format"];
                var endGroup = m.Groups["end"];

                values.Add(propertyGroup.Value == "0" ? source : Reflective.GetValue<object>(source, propertyGroup.Value));

                return new string('{', startGroup.Captures.Count) + (values.Count - 1) + formatGroup.Value + new string('}', endGroup.Captures.Count);
            });

            return string.Format(provider, rewrittenFormat, values.ToArray());
        }

        public static string ToBase64String(this string str)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
        }

        public static string FromBase64String(this string b64)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(b64));
        }

        public static string ToLowerCamelCase(this string str, bool ignorePeriods = false)
        {
            if (string.IsNullOrWhiteSpace(str) || str.Length <= 1)
            {
                return string.Empty;
            }

            if (!ignorePeriods && str.Contains("."))
            {
                return string.Join(".", str.Split('.').Select(s => ToLowerCamelCase(s)));
            }

            return char.ToLowerInvariant(str[0]) + str.Substring(1);
        }

        public static string StripHtml(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(value);

            return htmlDoc.DocumentNode.InnerText;
        }
    }
}