using System.IO;
using CMI.Contract.DocumentConverter;

namespace CMI.Manager.DocumentConverter
{
    public class Doc : IDoc
    {
        private readonly FileInfo fileInfo;

        public Doc(FileInfo fi, string identifier)
        {
            fileInfo = fi;
            Identifier = identifier;
            Stream = fi.OpenRead();
        }

        public Stream Stream { get; private set; }
        public string Identifier { get; }
        public string Extension => fileInfo.Extension;
        public string FileName => fileInfo.Name;

        public void Dispose()
        {
            if (Stream != null)
            {
                Stream.Dispose();
                Stream = null;
            }
        }
    }
}