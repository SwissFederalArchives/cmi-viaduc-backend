namespace CMI.Web.Frontend.api.Interfaces
{
    public interface IFileSystem
    {
        string[] ReadAllLines(string path);
        bool FileExists(string path);
    }
}