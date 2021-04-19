namespace CMI.Web.Common.Helpers
{
    public interface IDirectoryHelper
    {
        string ClientDefaultPath { get; }
        string StaticDefaultPath { get; }
        string StaticPagePath { get; }
        string IndexPagePath { get; }

        string ConfigDirectory { get; }
        string ClientConfigDirectory { get; }
    }
}