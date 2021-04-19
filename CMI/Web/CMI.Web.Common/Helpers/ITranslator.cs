namespace CMI.Web.Common.Helpers
{
    public interface ITranslator
    {
        string GetTranslation(string language, string path, string defaultText = null);
    }
}