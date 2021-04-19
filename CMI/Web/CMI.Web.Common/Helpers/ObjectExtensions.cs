using Newtonsoft.Json;

namespace CMI.Web.Common.Helpers
{
    public static class ObjectExtensions
    {
        public static bool IsEquivalent(this object a, object b)
        {
            if (a == null && b == null)
            {
                return true;
            }

            if (a == null)
            {
                return false;
            }

            var jsonA = JsonConvert.SerializeObject(a);
            var jsonb = JsonConvert.SerializeObject(b);
            return jsonA == jsonb;
        }
    }
}