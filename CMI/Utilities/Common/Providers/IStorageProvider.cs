using System;
using System.IO;
using System.Threading.Tasks;

namespace CMI.Utilities.Common.Providers
{
    public interface IStorageProvider
    {
        Task CopyFileAsync(FileInfo sourceFile, string relPath, string extension, string targetDirectory);

        Task<MemoryStream> ReadFileAsync(Uri fileUri);
    }
}
