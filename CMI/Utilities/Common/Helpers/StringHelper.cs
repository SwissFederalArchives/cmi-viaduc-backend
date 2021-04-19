using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Serilog;

namespace CMI.Utilities.Common.Helpers
{
    public class StringHelper
    {
        private static readonly Regex KeyToBlankRegex = new Regex(@"([:,;\-\/\?\!<>\\\*\|()\[\]{}""\x00-\x1f\x80-\x9f]+)");
        private static readonly Regex KeyCleanupRegex = new Regex(@"[ ]");

        /// <summary>
        ///     Trim and replace multiple censecutive occurences of whitespace chars
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string CleanupSuperfluousWhitespaces(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                s = CONSECUTIVE_WHITESPACES.Replace(s, " ");
                s = s.Trim();
            }

            return s;
        }

        /// <summary>
        ///     2 Strings mit Delimiter aneinanderhängen: s1 + delim + s2, unter Vermeidung von delimiter-Verdoppelung
        ///     D.h. "a/" + "/" + "/b" -> "a/b" (und nicht "a///b")
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="delim"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public static string AddToString(string s1, string delim, string s2)
        {
            var s = s1;
            if (string.IsNullOrEmpty(s1))
            {
                s = s2;
            }
            else if (!string.IsNullOrEmpty(s2))
            {
                if (string.IsNullOrEmpty(delim))
                {
                    s = s1 + s2;
                }
                else
                {
                    if (s2.StartsWith(delim))
                    {
                        s2 = s2.Remove(0, delim.Length);
                    }

                    s = s1 + (s1.EndsWith(delim) ? string.Empty : delim) + s2;
                }
            }

            return s;
        }

        /// <summary>
        ///     Spezialzeichen wie ä, ö, ü in die Basisform (ae, oe, ue) transformieren
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string TransformDiacritics(string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                var sb = new StringBuilder();
                var j = 0;
                var i = s.IndexOfAny(DiacriticsTransformationArray);
                while (i >= 0)
                {
                    var k = DiacriticsTransformationSequenced.IndexOf(s[i]);
                    if (i > j)
                    {
                        sb.Append(s.Substring(j, i - j));
                    }

                    sb.Append(DiacriticsTransformation[s[i]]);
                    j = i + 1;
                    i = s.IndexOfAny(DiacriticsTransformationArray, j);
                }

                if (s.Length > j)
                {
                    sb.Append(s.Substring(j, s.Length - j));
                }

                s = sb.ToString();
            }

            return s;
        }

        /// <summary>
        ///     Alle akzentuierten Characters mit dem Basis-Character ersetzen
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string NormalizeDiacritics(string s, bool transformFirst)
        {
            if (!string.IsNullOrEmpty(s))
            {
                var sb = new StringBuilder();
                s = transformFirst ? TransformDiacritics(s) : s;
                var normalizedString = s.Normalize(NormalizationForm.FormD);
                for (var i = 0; i < normalizedString.Length; i++)
                {
                    var c = normalizedString[i];
                    var cc = CharUnicodeInfo.GetUnicodeCategory(c);
                    if (cc != UnicodeCategory.NonSpacingMark)
                    {
                        sb.Append(c);
                    }
                }

                s = sb.ToString();
            }

            return s;
        }

        public static string NormalizeDiacritics(string s)
        {
            return NormalizeDiacritics(s, true);
        }


        public static string ReplaceDiacritics(string s)
        {
            return NormalizeDiacritics(s, true);
        }

        public static string ToIdentifier(string s, bool lowerCase = false)
        {
            if (!string.IsNullOrEmpty(s))
            {
                s = ReplaceDiacritics(s);
                s = DefaultIdentifierToBlankRegex.Replace(s, "_");
                s = DefaultIdentifierCleanupRegex.Replace(s, "");
                if (lowerCase)
                {
                    s = s.ToLowerInvariant();
                }
            }

            return s;
        }

        public static string GetNormalizedKey(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                key = ReplaceDiacritics(key);
                key = KeyToBlankRegex.Replace(key, "_");
                key = KeyCleanupRegex.Replace(key, "");
            }

            return key;
        }

        #region Globals

        private static readonly Regex CONSECUTIVE_WHITESPACES = new Regex(@"\s+");

        private static readonly Dictionary<char, string> DiacriticsTransformation = new Dictionary<char, string>();
        private static readonly char[] DiacriticsTransformationArray;
        private static readonly string DiacriticsTransformationSequenced;

        private static readonly Regex DefaultIdentifierToBlankRegex = new Regex(@"([:,;\-\/\?\!<>\\\*\|()\[\]{}""\x00-\x1f\x80-\x9f]|\.+)");
        private static readonly Regex DefaultIdentifierCleanupRegex = new Regex(@"[ ]");

        static StringHelper()
        {
            var repl = string.Empty;
            var norm = string.Empty;

            repl += "É|Ê|Ë|š|Ì|Í|ƒ|œ|µ|Î|Ï|ž|Ð|Ÿ|Ñ|Ò|Ó|Ô|Š|£|Õ|Ö |Œ|¥|Ø|Ž|§|À|Ù|Á|Ú|Â|Û|Ã|Ü |Ä |Ý|";
            norm += "E|E|E|s|I|I|f|o|m|I|I|z|D|Y|N|O|O|O|S|L|O|OE|O|Y|O|Z|S|A|U|A|U|A|U|A|UE|AE|Y|";

            repl += "Å|Æ|ß|Ç|à|È|á|â|û|Ĕ|ĭ|ņ|ş|Ÿ|ã|ü |ĕ|Į|Ň|Š|Ź|ä |ý|Ė|į|ň|š|ź|å|þ|ė|İ|ŉ|Ţ|Ż|æ|ÿ|";
            norm += "A|A|S|C|a|E|a|a|u|E|i|n|s|Y|a|ue|e|I|N|S|Z|ae|y|E|i|n|s|z|a|p|e|I|n|T|Z|a|y|";

            repl += "Ę|ı|Ŋ|ţ|ż|ç|Ā|ę|Ĳ|ŋ|Ť|Ž|è|ā|Ě|ĳ|Ō|ť|ž|é|Ă|ě|Ĵ|ō|Ŧ|ſ|ê|ă|Ĝ|ĵ|Ŏ|ŧ|ë|Ą|ĝ|Ķ|ŏ|";
            norm += "E|l|n|t|z|c|A|e|I|n|T|Z|e|a|E|i|O|t|z|e|A|e|J|o|T|i|e|a|G|j|O|t|e|A|g|K|o|";

            repl += "Ũ|ì|ą|Ğ|ķ|Ő|ũ|í|Ć|ğ|ĸ|ő|Ū|î|ć|Ġ|Ĺ|Œ|ū|ï|Ĉ|ġ|ĺ|œ|Ŭ|ð|ĉ|Ģ|Ļ|Ŕ|ŭ|ñ|Ċ|ģ|ļ|ŕ|Ů|";
            norm += "U|i|a|G|k|O|u|i|C|g|k|o|U|i|c|G|L|O|u|i|C|g|l|o|U|o|c|G|L|R|u|n|C|g|l|r|U|";

            repl += "ò|ċ|Ĥ|Ľ|Ŗ|ů|ó|Č|ĥ|ľ|ŗ|Ű|ô|č|Ħ|Ŀ|Ř|ű|õ|Ď|ħ|ŀ|ř|Ų|ö |ď|Ĩ|Ł|Ś|ų|Đ|ĩ|ł|ś|Ŵ|ø|đ|";
            norm += "o|c|H|L|R|u|o|C|h|l|r|U|o|c|H|L|R|u|o|D|h|l|r|U|oe|d|I|L|S|c|D|i|l|s|W|o|d|";

            repl += "Ī|Ń|Ŝ|ŵ|ù|Ē|ī|ń|ŝ|Ŷ|Ə|ú|ē|Ĭ|Ņ|Ş|ŷ";
            norm += "I|N|S|w|u|E|i|n|s|Y|e|u|e|I|N|S|y";

            var rs = repl.Split('|');
            var ns = norm.Split('|');

            for (var i = 0; i < rs.Length; i++)
            {
                var c = rs[i].ToCharArray()[0];
                if (!DiacriticsTransformation.ContainsKey(c))
                {
                    DiacriticsTransformation.Add(c, ns[i]);
                }
                else
                {
                    Log.Information("DiacriticsTransformation already contains an entry ({entry}) for {char}", DiacriticsTransformation[c], c);
                }
            }

            DiacriticsTransformationArray = new char[DiacriticsTransformation.Keys.Count];
            DiacriticsTransformation.Keys.CopyTo(DiacriticsTransformationArray, 0);
            DiacriticsTransformationSequenced = DiacriticsTransformationArray.ToString();
        }

        #endregion
    }
}