using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace CMI.Tools.AnonymizeServiceMock
{
    public class ApiKeyChecker
    {
        public static string Key { get; set; }

        public static bool IsCorrect(HttpRequestMessage request)
        {
            if (request.Headers.TryGetValues("X-ApiKey", out var values))
            {
                var value = values.FirstOrDefault();
                return value == Key;
            }

            return false;
        }
    }
}
