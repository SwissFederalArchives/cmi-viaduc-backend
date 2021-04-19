using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace CMI.Engine.Security
{
    public class PasswordHelper
    {
        // We have a list of 256 possible chars available to build a password
        // The order of chars is randomized. The list contains duplicates
        private readonly char[] passwordChars =
        {
            'b', '+', '6', '=', '#', '_', '=', 'B', '&', '$', 'p', '$', '!', 'O', 'y', '7', '#', 'M', 'S', '?', 'd', '=', '%',
            'X', 'u', 'q', 'o', 'I', 'i', 'm', 'g', 'r', 'Z', '%', 'e', '!', 'p', 'R', 'B', 'Q', 'a', 'e', '0', '+', 'I', 'G',
            'J', 'E', '?', '@', '3', 'x', '&', '_', 'J', 'R', 'i', 's', 'f', 'b', '@', 'M', '-', '8', 'D', 'j', 'V', 'P', 'H',
            'L', 'P', '@', 'T', '/', 'u', 'j', '2', '/', 'C', '#', 'l', '8', '+', '=', 'g', 'L', '1', 'a', 'H', '!', 'r', '&',
            '8', '3', '/', 'W', 'e', 'N', 'I', 'v', 'V', 'O', '$', 'L', '?', '%', 'T', 'V', 'M', '9', 'm', 'P', 'K', 'i', 'w',
            'D', '-', '1', 'A', 'x', 'y', 'F', 'N', '2', 'S', 'N', '=', 'Z', '!', 'n', 'h', 'd', 'K', '#', 'R', 'c', 'n', 'Q',
            '?', 'c', '%', 'K', 'n', '-', '%', 's', 'v', '0', 'X', 'F', '5', '7', '+', 'J', 'Y', 'U', 'q', 'B', '6', 'C', '5',
            'o', 'g', 'x', '@', 't', 'd', '#', 'X', '%', 'b', '-', 'l', '#', 'U', 'O', '3', '7', 'q', 'f', 'E', 'G', 'F', 'W',
            '$', 'z', 'D', 'H', 'z', 'Q', 'j', '/', 'o', 'a', 'Y', '6', '-', 'h', '4', '+', 'z', '&', 'k', 'U', 'k', '?', '!',
            't', 'E', '_', 'W', 'h', '=', '2', 'Y', 'l', '5', 'w', 't', 'A', 'G', '@', 'f', '$', '-', 'c', 'S', 'm', '/', '4',
            '?', '9', '9', 'A', 'Z', 'r', '+', 'T', '&', 'k', '&', '!', 'y', 'w', '$', '1', 'p', 'C', '0', 'v', '/', 'u', 's',
            '4', '*', '\\'
        };

        private readonly string seed;

        public PasswordHelper(string seed)
        {
            this.seed = seed;
        }

        /// <summary>
        ///     Gets the password given a string.
        ///     Password is generated using a hash of the input string
        ///     Requirements for the password are
        ///     1. Unique for different input (nor required to be 100% unique)
        ///     2. Long enouqh to be considered a string password
        ///     3. Recreatable
        /// </summary>
        /// <param name="recordId">The record identifier.</param>
        /// <param name="passwordLength">Length of the password. maximum is 32</param>
        /// <returns>System.String.</returns>
        public string GetHashPassword(string recordId, int passwordLength = 20)
        {
            var textToHash = $"{seed}{recordId}{string.Join("", seed.Reverse())}{string.Join("", recordId.Reverse())}";

            // Get the hash
            var hash = GetSha256Hash(textToHash);

            // Create a nifty password
            var sb = new StringBuilder();
            for (var index = 0; index < (passwordLength > hash.Length ? hash.Length : passwordLength); index++)
            {
                sb.Append(passwordChars[hash[index]]);
            }

            // Return the hexadecimal string.
            return sb.ToString();
        }

        private static byte[] GetSha256Hash(string textToHash)
        {
            var sha256 = new SHA256CryptoServiceProvider();
            var hashBytes = Encoding.UTF8.GetBytes(textToHash);
            var hash = sha256.ComputeHash(hashBytes);
            return hash;
        }
    }
}