using System.Linq;
using System.Text.RegularExpressions;
using CMI.Manager.DocumentConverter.Extraction.Interfaces;

namespace CMI.Manager.DocumentConverter.Extraction
{
    public class AllowedExtensionFilter
    {
        private static string filterstring;
        private static readonly object lockObj = new object();

        private readonly ITextExtractorSettings settings;

        public AllowedExtensionFilter(ITextExtractorSettings settings)
        {
            this.settings = settings;
        }

        public bool IsAllowed(string extension)
        {
            lock (lockObj)
            {
                if (filterstring == null)
                {
                    filterstring = settings.ExplicitAllowedDocExtensions;
                }
            }

            if (string.IsNullOrWhiteSpace(filterstring))
            {
                return true;
            }

            var ext = extension.StartsWith(".") ? extension.Substring(1) : extension;

            return filterstring.Split(',').Select(s => s.Trim()).Where(s => s != string.Empty).Any(filter => IsMatch(ext, filter));
        }

        private static bool IsMatch(string input, string filter)
        {
            var pattern = "^" + Regex.Escape(filter)
                                  .Replace(@"\*", ".*")
                                  .Replace(@"\?", ".")
                              + "$";

            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            return regex.IsMatch(input);
        }
    }
}