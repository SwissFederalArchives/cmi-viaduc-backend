using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CMI.Engine.Asset.PostProcess
{
    public static class PathHelper
    {
        private static readonly string reservedCharPattern = @"[\s!*'\(\);:@&=+$,\/\?%#\[\]]";
        public static string CreateShortValidUrlName(string fileOrPath, bool isFile)
        {
            if (string.IsNullOrEmpty(fileOrPath))
            {
                return fileOrPath;
            }

            var driveLetter = Path.GetPathRoot(fileOrPath);
            var extension = isFile ? GetExtension(fileOrPath) : "";
            var fileName = fileOrPath.Substring(driveLetter.Length, fileOrPath.Length - extension.Length - driveLetter.Length);

            var parts = fileName.Split(new[] {'\\', '/'});
            var retVal = string.Empty;
            foreach (var part in parts)
            {
                // Replace spaces with underscores, so the url and filenames are equal
                // Also replace all reserved chars for URL
                var regex = new Regex(reservedCharPattern);
                var newName = RemoveDiacritics(regex.Replace(part, "_"));

                // Disallow large file or path names
                // Add a hash to make the shortened file name unique
                if (newName.Length > 40)
                {
                    var hash = Hash(newName);
                    newName = newName.Substring(0, 33) + "_" + hash;
                }

                retVal = Path.Combine(retVal, newName);
            }

            return Path.Combine(driveLetter, retVal + extension);
        }

        /// <summary>
        /// Create a sub path list for a given numeric id.
        /// This is to distribute a large list of items into several subfolders
        /// </summary>
        /// <param name="archiveId"></param>
        /// <returns></returns>
        public static List<PathItem> ArchiveIdToPathSegments(string archiveId)
        {
            var retVal = new List<PathItem>();

            // Max length of scope id is 10 digits. As we like to split in 3 parts, we pad for 12
            var id = archiveId.PadLeft(12, '0');

            for (var i = 0; i < 3; i++)
            {
                retVal.Add(new (id.Substring(i*4, 4), id.Substring(i * 4, 4)));
            }

            return retVal;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

            for (int i = 0; i < normalizedString.Length; i++)
            {
                char c = normalizedString[i];
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        private static string Hash(string input)
        {
            using (SHA1Managed sha1 = new SHA1Managed())
            {
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
                var sb = new StringBuilder(hash.Length * 2);

                foreach (byte b in hash)
                {
                    // can be "x2" if you want lowercase
                    sb.Append(b.ToString("X2"));
                }

                // We return only the first 6 chars that are statistically quite unique for our purpose
                // https://stackoverflow.com/questions/18464517/6-character-short-hash-algorithm
                return sb.ToString(0, 6);
            }
        }

        private static string GetExtension(string inputString)
        {
            // Copied from FileInfo class
            int length = inputString.Length;
            int num = length;
            while (--num >= 0)
            {
                char ch = inputString[num];
                if (ch == '.')
                    return inputString.Substring(num, length - num);
                if ((int) ch == (int) Path.DirectorySeparatorChar || (int) ch == (int) Path.AltDirectorySeparatorChar ||
                    (int) ch == (int) Path.VolumeSeparatorChar)
                    break;
            }

            return string.Empty;
        }
    }
}
