using System.Collections.Generic;
using CMI.Web.Frontend.api.Interfaces;

namespace CMI.Web.Frontend.api
{
    public class MockFileSystem : IFileSystem
    {
        private readonly Dictionary<string, string[]> filesByPath = new Dictionary<string, string[]>();

        public string[] ReadAllLines(string path)
        {
            return filesByPath[path];
        }

        public bool FileExists(string path)
        {
            return filesByPath.ContainsKey(path);
        }

        public void AddFile(string path, string[] content)
        {
            filesByPath.Add(path, content);
        }
    }
}