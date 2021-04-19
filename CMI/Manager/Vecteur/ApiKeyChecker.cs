using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace CMI.Manager.Vecteur
{
    public static class ApiKeyChecker
    {
        public static string Key { get; set; }

        // Im Moment für den Mock noch hartcodiert
        public static bool IsCorrect(HttpRequestMessage request)
        {
            IEnumerable<string> values;
            if (request.Headers.TryGetValues("X-ApiKey", out values))
            {
                var value = values.FirstOrDefault();
                return value == Key;
            }

            return false;
        }
    }
}