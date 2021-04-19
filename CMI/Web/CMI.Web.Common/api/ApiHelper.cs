namespace CMI.Web.Common.api
{
    public static class ApiHelper
    {
        static ApiHelper()
        {
            UrlApiPart = "/" + WebApiSubRoot.ToLower() + "/";
        }

        public static bool IsApiRequest(string requestUrl)
        {
            var s = requestUrl;
            if (s != null)
            {
                var i = s.IndexOf("?");
                if (i >= 0)
                {
                    s = s.Substring(0, i);
                }

                return s.ToLower().IndexOf(UrlApiPart) >= 0;
            }

            return false;
        }

        #region Constants & Properties

        public const string WebApiSubRoot = "api";
        private static readonly string UrlApiPart = "api";

        #endregion
    }
}