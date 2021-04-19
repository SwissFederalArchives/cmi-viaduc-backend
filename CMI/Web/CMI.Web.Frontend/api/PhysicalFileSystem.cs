using System.IO;
using System.Text;
using CMI.Web.Frontend.api.Interfaces;

namespace CMI.Web.Frontend.api
{
    public class PhysicalFileSystem : IFileSystem
    {
        public string[] ReadAllLines(string path)
        {
            return File.ReadAllLines(path, Encoding.UTF8);
        }

        public bool FileExists(string path)
        {
            return File.Exists(path);
        }
    }
}