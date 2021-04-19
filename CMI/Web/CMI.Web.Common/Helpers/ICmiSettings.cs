namespace CMI.Web.Common.Helpers
{
    public interface ICmiSettings
    {
        string this[string name] { get; }
    }
}