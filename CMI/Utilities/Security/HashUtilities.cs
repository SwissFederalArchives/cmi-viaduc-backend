using System;
using System.Security.Cryptography;
using System.Text;

namespace CMI.Utilities.Security
{
    public class HashUtilities
    {
        /// <summary>
        ///     Returns a MD5 hash for a given string.
        /// </summary>
        /// <param name="textToHash">The text to hash.</param>
        /// <returns>The hash code.</returns>
        public static string GetMd5Hash(string textToHash)
        {
            // Check if we have data to hash.
            if (string.IsNullOrEmpty(textToHash))
            {
                return string.Empty;
            }

            // Calculate MD5. 
            MD5 md5 = new MD5CryptoServiceProvider();
            var hashBytes = Encoding.Default.GetBytes(textToHash);
            var result = md5.ComputeHash(hashBytes);

            return BitConverter.ToString(result);
        }
    }
}