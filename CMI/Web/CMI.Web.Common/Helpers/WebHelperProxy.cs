namespace CMI.Web.Common.Helpers
{
    public class WebCmiConfigProvider : IWebCmiConfigProvider
    {
        public string GetStringSetting(string key, string defaultValue = null)
        {
            return WebHelper.GetStringSetting(key, defaultValue);
        }
    }
}