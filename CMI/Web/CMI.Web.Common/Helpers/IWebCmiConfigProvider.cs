namespace CMI.Web.Common.Helpers
{
    public interface IWebCmiConfigProvider
    {
        string GetStringSetting(string key, string defaultValue = null);
    }
}